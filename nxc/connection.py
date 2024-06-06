import random
from os.path import isfile
from threading import BoundedSemaphore
from functools import wraps
from time import sleep
from ipaddress import ip_address
from dns import resolver, rdatatype
from socket import AF_UNSPEC, SOCK_DGRAM, IPPROTO_IP, AI_CANONNAME, getaddrinfo

from nxc.config import pwned_label
from nxc.helpers.logger import highlight
from nxc.loaders.moduleloader import ModuleLoader
from nxc.logger import nxc_logger, NXCAdapter
from nxc.context import Context
from nxc.credentials import credentials
from nxc.protocols.ldap.laps import laps_search

from impacket.dcerpc.v5 import transport
import sys
import contextlib

sem = BoundedSemaphore(1)
global_failed_logins = 0
user_failed_logins = {}


def get_host_addr_info(target, force_ipv6, dns_server, dns_tcp, dns_timeout):
    result = {
        "host": "",
        "is_ipv6": False,
        "is_link_local_ipv6": False
    }
    address_info = {"AF_INET6": "", "AF_INET": ""}

    try:
        if ip_address(target).version == 4:
            address_info["AF_INET"] = target
        else:
            address_info["AF_INET6"] = target
    except Exception:
        # If the target is not an IP address, we need to resolve it
        if not (dns_server or dns_tcp):
            for res in getaddrinfo(target, None, AF_UNSPEC, SOCK_DGRAM, IPPROTO_IP, AI_CANONNAME):
                af, _, _, canonname, sa = res
                address_info[af.name] = sa[0]

            if address_info["AF_INET6"] and ip_address(address_info["AF_INET6"]).is_link_local:
                address_info["AF_INET6"] = canonname
                result["is_link_local_ipv6"] = True
        else:
            dnsresolver = resolver.Resolver()
            dnsresolver.timeout = dns_timeout
            dnsresolver.lifetime = dns_timeout

            if dns_server:
                dnsresolver.nameservers = [dns_server]

            try:
                answers_ipv4 = dnsresolver.resolve(target, rdatatype.A, raise_on_no_answer=False, tcp=dns_tcp)
                address_info["AF_INET"] = answers_ipv4[0].address
            except Exception:
                pass

            try:
                answers_ipv6 = dnsresolver.resolve(target, rdatatype.AAAA, raise_on_no_answer=False, tcp=dns_tcp)
                address_info["AF_INET6"] = answers_ipv6[0].address

                if address_info["AF_INET6"] and ip_address(address_info["AF_INET6"]).is_link_local:
                    result["is_link_local_ipv6"] = True
            except Exception:
                pass

    if not (address_info["AF_INET"] or address_info["AF_INET6"]):
        raise Exception(f"The DNS query name does not exist: {target}")

    # IPv4 preferred
    if address_info["AF_INET"] and not force_ipv6:
        result["host"] = address_info["AF_INET"]
    else:
        result["is_ipv6"] = True
        result["host"] = address_info["AF_INET6"]

    return result


def requires_admin(func):
    def _decorator(self, *args, **kwargs):
        if self.admin_privs is False:
            return None
        return func(self, *args, **kwargs)

    return wraps(func)(_decorator)


def dcom_FirewallChecker(iInterface, remoteHost, timeout):
    stringBindings = iInterface.get_cinstance().get_string_bindings()
    for strBinding in stringBindings:
        if strBinding["wTowerId"] == 7:
            if strBinding["aNetworkAddr"].find("[") >= 0:
                binding, _, bindingPort = strBinding["aNetworkAddr"].partition("[")
                bindingPort = "[" + bindingPort
            else:
                binding = strBinding["aNetworkAddr"]
                bindingPort = ""

            if binding.upper().find(iInterface.get_target().upper()) >= 0:
                stringBinding = "ncacn_ip_tcp:" + strBinding["aNetworkAddr"][:-1]
                break
            elif iInterface.is_fqdn() and binding.upper().find(iInterface.get_target().upper().partition(".")[0]) >= 0:
                stringBinding = f"ncacn_ip_tcp:{iInterface.get_target()}{bindingPort}"
    if "stringBinding" not in locals():
        return True, None
    try:
        rpctransport = transport.DCERPCTransportFactory(stringBinding)
        rpctransport.setRemoteHost(remoteHost)
        rpctransport.set_connect_timeout(timeout)
        rpctransport.connect()
        rpctransport.disconnect()
    except Exception as e:
        nxc_logger.debug(f"Exception while connecting to {stringBinding}: {e}")
        return False, stringBinding
    else:
        return True, stringBinding


class connection:
    def __init__(self, args, db, target):
        self.args = args
        self.db = db
        self.logger = nxc_logger
        self.conn = None

        # Authentication info
        self.password = ""
        self.username = ""
        self.kerberos = bool(self.args.kerberos or self.args.use_kcache or self.args.aesKey)
        self.aesKey = None if not self.args.aesKey else self.args.aesKey[0]
        self.use_kcache = None if not self.args.use_kcache else self.args.use_kcache
        self.admin_privs = False
        self.failed_logins = 0

        # Network info
        self.domain = None
        self.host = None            # IP address of the target. If kerberos this is the hostname
        self.hostname = target      # Target info supplied by the user, may be an IP address or a hostname
        self.remoteName = target    # hostname + domain, defaults to target if domain could not be resolved/not specified
        self.kdcHost = self.args.kdcHost
        self.port = self.args.port
        self.local_ip = None

        # DNS resolution
        dns_result = self.resolver(target)
        if dns_result:
            self.host, self.is_ipv6, self.is_link_local_ipv6 = dns_result["host"], dns_result["is_ipv6"], dns_result["is_link_local_ipv6"]
        else:
            return

        if self.args.kerberos:
            self.host = self.hostname

        self.logger.info(f"Socket info: host={self.host}, hostname={self.hostname}, kerberos={self.kerberos}, ipv6={self.is_ipv6}, link-local ipv6={self.is_link_local_ipv6}")

        try:
            self.proto_flow()
        except Exception as e:
            if "ERROR_DEPENDENT_SERVICES_RUNNING" in str(e):
                self.logger.error(f"Exception while calling proto_flow() on target {target}: {e}")
            else:
                self.logger.exception(f"Exception while calling proto_flow() on target {target}: {e}")
        finally:
            self.logger.debug(f"Closing connection to: {target}")
            with contextlib.suppress(Exception):
                self.conn.close()

    def resolver(self, target):
        try:
            return get_host_addr_info(
                target=target,
                force_ipv6=self.args.force_ipv6,
                dns_server=self.args.dns_server,
                dns_tcp=self.args.dns_tcp,
                dns_timeout=self.args.dns_timeout
            )
        except Exception as e:
            self.logger.info(f"Error resolving hostname {target}: {e}")
            return None

    @staticmethod
    def proto_args(std_parser, module_parser):
        return

    def proto_logger(self):
        pass

    def enum_host_info(self):
        return

    def print_host_info(self):
        return

    def create_conn_obj(self):
        return

    def check_if_admin(self):
        return

    def kerberos_login(self, domain, username, password="", ntlm_hash="", aesKey="", kdcHost="", useCache=False):
        return

    def plaintext_login(self, domain, username, password):
        return

    def hash_login(self, domain, username, ntlm_hash):
        return

    def proto_flow(self):
        self.logger.debug("Kicking off proto_flow")
        self.proto_logger()
        if not self.create_conn_obj():
            self.logger.info(f"Failed to create connection object for target {self.host}, exiting...")
        else:
            self.logger.debug("Created connection object")
            self.enum_host_info()
            if self.print_host_info() and (self.login() or (self.username == "" and self.password == "")):
                if hasattr(self.args, "module") and self.args.module:
                    self.load_modules()
                    self.logger.debug("Calling modules")
                    self.call_modules()
                else:
                    self.logger.debug("Calling command arguments")
                    self.call_cmd_args()

    def call_cmd_args(self):
        """Calls all the methods specified by the command line arguments

        Iterates over the attributes of an object (self.args)
        For each attribute, it checks if the object (self) has an attribute with the same name and if that attribute is callable (i.e., a function)
        If both conditions are met and the attribute value is not False or None,
        it calls the function and logs a debug message

        Parameters
        ----------
            self (object): The instance of the class.

        Returns
        -------
            None
        """
        for attr, value in vars(self.args).items():
            if hasattr(self, attr) and callable(getattr(self, attr)) and value is not False and value is not None:
                self.logger.debug(f"Calling {attr}()")
                getattr(self, attr)()

    def call_modules(self):
        """Calls modules and performs various actions based on the module's attributes.

        It iterates over the modules specified in the command line arguments.
        For each module, it loads the module and creates a context object, then calls functions based on the module's attributes.
        """
        for module in self.modules:
            self.logger.debug(f"Loading module {module.name} - {module}")
            module_logger = NXCAdapter(
                extra={
                    "module_name": module.name.upper(),
                    "host": self.host,
                    "port": self.args.port,
                    "hostname": self.hostname,
                },
            )

            self.logger.debug(f"Loading context for module {module.name} - {module}")
            context = Context(self.db, module_logger, self.args)
            context.localip = self.local_ip

            if hasattr(module, "on_request") or hasattr(module, "has_response"):
                self.logger.debug(f"Module {module.name} has on_request or has_response methods")
                self.server.connection = self
                self.server.context.localip = self.local_ip

            if hasattr(module, "on_login"):
                self.logger.debug(f"Module {module.name} has on_login method")
                module.on_login(context, self)

            if self.admin_privs and hasattr(module, "on_admin_login"):
                self.logger.debug(f"Module {module.name} has on_admin_login method")
                module.on_admin_login(context, self)

            if (not hasattr(module, "on_request") and not hasattr(module, "has_response")) and hasattr(module, "on_shutdown"):
                self.logger.debug(f"Module {module.name} has on_shutdown method")
                module.on_shutdown(context, self)

    def inc_failed_login(self, username):
        global global_failed_logins
        global user_failed_logins

        if username not in user_failed_logins:
            user_failed_logins[username] = 0

        user_failed_logins[username] += 1
        global_failed_logins += 1
        self.failed_logins += 1

    def over_fail_limit(self, username):
        global global_failed_logins
        global user_failed_logins

        if global_failed_logins == self.args.gfail_limit:
            return True

        if self.failed_logins == self.args.fail_limit:
            return True

        if username in user_failed_logins and self.args.ufail_limit == user_failed_logins[username]:
            return True

        return False

    def try_credentials(self, domain, username, owned, secret, cred_type, data=None):
        """
        Try to login using the specified credentials and protocol.
        With  --jitter an authentication throttle can be applied.

        Possible login methods are:
            - plaintext (/kerberos)
            - NTLM-hash (/kerberos)
            - AES-key
        """
        if self.over_fail_limit(username):
            return False
        if self.args.continue_on_success and owned:
            return False
        if hasattr(self.args, "delegate") and self.args.delegate:
            self.args.kerberos = True

        if self.args.jitter:
            jitter = self.args.jitter
            if "-" in jitter:
                start, end = jitter.split("-")
                jitter = (int(start), int(end))
            else:
                jitter = (0, int(jitter))
            value = jitter[0] if jitter[0] == jitter[1] else random.choice(range(jitter[0], jitter[1]))
            self.logger.debug(f"Throttle authentications: sleeping {value} second(s)")
            sleep(value)

        with sem:
            if cred_type == "plaintext":
                if self.args.kerberos:
                    self.logger.debug("Trying to authenticate using Kerberos")
                    return self.kerberos_login(domain, username, secret, "", "", self.kdcHost, False)
                elif hasattr(self.args, "domain"):  # Some protocols don't use domain for login
                    self.logger.debug("Trying to authenticate using plaintext with domain")
                    return self.plaintext_login(domain, username, secret)
                elif self.args.protocol == "ssh":
                    self.logger.debug("Trying to authenticate using plaintext over SSH")
                    return self.plaintext_login(username, secret, data)
                else:
                    self.logger.debug("Trying to authenticate using plaintext")
                    return self.plaintext_login(username, secret)
            elif cred_type == "hash":
                if self.args.kerberos:
                    return self.kerberos_login(domain, username, "", secret, "", self.kdcHost, False)
                return self.hash_login(domain, username, secret)
            elif cred_type == "aesKey":
                return self.kerberos_login(domain, username, "", "", secret, self.kdcHost, False)

    def login(self):
        """Try to login using the credentials specified in the command line or in the database.

        :return: True if the login was successful, False otherwise.
        """

        creds = credentials(self.args, self.db)

        if self.args.use_kcache:
            self.logger.debug("Trying to authenticate using Kerberos cache")
            with sem:
                username = creds.usernames[0] if len(creds.usernames) else ""
                password = creds.passwords[0] if len(creds.passwords) else ""
                domain = creds.domains[0] if len(creds.domains) else ""
                if (self.kerberos_login(domain, username, password, "", "", self.kdcHost, True)):
                    self.logger.info("Successfully authenticated using Kerberos cache")
                    return True

        if hasattr(self.args, "laps") and self.args.laps:
            self.logger.debug("Trying to authenticate using LAPS")
            creds.usernames[0], creds.secrets[0], creds.domains[0], ntlm_hash = laps_search(self, username, secret, cred_type, domain)
            cred_type = ["plaintext"]
            if not (username[0] or secret[0] or domain[0]):
                return False

        owned = [False]* len(creds.usernames)  # Determines whether we have found a valid credential for this user. Default: False
        if not self.args.no_bruteforce:
            for secr_index, secr in enumerate(creds.secrets):
                for user_index, user in enumerate(creds.usernames):
                    if self.try_credentials(creds.domains[user_index], user, owned[user_index], secr, creds.cred_types[secr_index], creds.data[secr_index]):
                        owned[user_index] = True
                        if not self.args.continue_on_success:
                            return True
        else:
            if len(creds.usernames) != len(creds.secrets):
                self.logger.error("Number provided of usernames and passwords/hashes do not match!")
                return False
            for user_index, user in enumerate(creds.usernames):
                if self.try_credentials(creds.domains[user_index], user, owned[user_index], creds.secrets[user_index], creds.cred_types[user_index], creds.data[user_index]):
                    owned[user_index] = True
                    if not self.args.continue_on_success:
                        return True

        return True in owned

    def mark_pwned(self):
        return highlight(f"({pwned_label})" if self.admin_privs else "")

    def load_modules(self):
        self.logger.info(f"Loading modules for target: {self.host}")
        loader = ModuleLoader(self.args, self.db, self.logger)
        self.modules = []

        for module_path in self.module_paths:
            module = loader.init_module(module_path)
            self.modules.append(module)
