using System;
using System.Runtime.InteropServices;

using DInvoke.Data;

namespace Drone.Interop;

using static Data;

public static class Delegates
{
    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true, CharSet = CharSet.Unicode)]
    public delegate bool CreateProcessW(
        [MarshalAs(UnmanagedType.LPWStr)] string applicationName,
        [MarshalAs(UnmanagedType.LPWStr)] string commandLine,
        ref SECURITY_ATTRIBUTES processAttributes,
        ref SECURITY_ATTRIBUTES threadAttributes,
        bool inheritHandles,
        PROCESS_CREATION_FLAGS creationFlags,
        IntPtr environment,
        [MarshalAs(UnmanagedType.LPWStr)] string currentDirectory,
        STARTUPINFOW startupInfo,
        out PROCESS_INFORMATION processInformation);

    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
    public delegate bool TerminateProcess(
        IntPtr hProcess,
        uint exitCode);

    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
    public delegate uint QueueUserApc(
        IntPtr fnApc,
        IntPtr hThread,
        IntPtr data);

    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
    public delegate uint ResumeThread(IntPtr hThread);
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true, CharSet = CharSet.Unicode)]
    public delegate bool LogonUserW(
        [MarshalAs(UnmanagedType.LPWStr)] string lpszUsername,
        [MarshalAs(UnmanagedType.LPWStr)] string lpszDomain,
        [MarshalAs(UnmanagedType.LPWStr)] string lpszPassword,
        LOGON_USER_TYPE dwLogonType,
        LOGON_USER_PROVIDER dwLogonProvider,
        out IntPtr phToken);

    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
    public delegate bool ImpersonateLoggedOnUser(IntPtr hToken);
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
    public delegate bool DuplicateTokenEx(
        IntPtr hExistingToken,
        uint dwDesiredAccess,
        ref SECURITY_ATTRIBUTES lpTokenAttributes,
        SECURITY_IMPERSONATION_LEVEL impersonationLevel,
        TOKEN_TYPE tokenType,
        out IntPtr phNewToken);
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
    public delegate bool RevertToSelf();
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
    public delegate IntPtr GetStdHandle(int nStdHandle);
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
    public delegate bool SetStdHandle(
        int nStdHandle,
        IntPtr hHandle);
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true, CharSet = CharSet.Unicode)]
    public delegate IntPtr GetCommandLineW();

    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
    public delegate bool CreatePipe(
        out IntPtr hReadPipe,
        out IntPtr hWritePipe,
        ref SECURITY_ATTRIBUTES lpPipeAttributes,
        uint nSize);
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
    public delegate bool ReadFile(
        IntPtr hFile,
        byte[] lpBuffer,
        uint nNumberOfBytesToRead,
        out uint lpNumberOfBytesRead,
        IntPtr lpOverlapped);

    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
    public delegate bool PeekNamedPipe(
        IntPtr hNamedPipe,
        IntPtr lpBuffer,
        IntPtr nBufferSize,
        IntPtr lpBytesRead,
        ref uint lpTotalBytesAvail,
        IntPtr lpBytesLeftThisMessage);

    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
    public delegate IntPtr CreateFileW(
        [MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
    public delegate uint GetFileAttributesW(IntPtr lpFileName);

    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
    public delegate bool GetFileAttributesExW(
        IntPtr lpFileName,
        uint fInfoLevelId,
        IntPtr lpFileInformation);

    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
    public delegate bool GetFileInformationByHandle(
        IntPtr hFile,
        IntPtr lpFileInformation);
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
    public delegate bool WriteFile(
        IntPtr hFile,
        byte[] lpBuffer,
        uint nNumberOfBytesToWrite,
        out uint lpNumberOfBytesWritten,
        IntPtr lpOverlapped);
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
    public delegate IntPtr CreateTransaction(
        IntPtr lpTransactionAttributes,
        IntPtr uow,
        int createOptions,
        int isolationLevel,
        int isolationFlags,
        int timeout,
        [MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder description);
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true, CharSet = CharSet.Unicode)]
    public delegate IntPtr CreateFileTransactedW(
        [MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile,
        IntPtr hTransaction,
        ref ushort pusMiniVersion,
        IntPtr nullValue);
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
    public delegate bool CloseHandle(IntPtr hObject);
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
    public delegate bool OpenProcessToken(
        IntPtr processHandle,
        TOKEN_ACCESS desiredAccess,
        out IntPtr tokenHandle);
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
    public delegate bool FreeLibrary(IntPtr hModule);
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
    public delegate uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
    public delegate IntPtr OpenSCManagerW(
        [MarshalAs(UnmanagedType.LPWStr)] string machineName,
        [MarshalAs(UnmanagedType.LPWStr)] string databaseName,
        SCM_ACCESS_RIGHTS desiredAccess);

    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
    public delegate IntPtr CreateServiceW(
        IntPtr hSCManager,
        [MarshalAs(UnmanagedType.LPWStr)] string serviceName,
        [MarshalAs(UnmanagedType.LPWStr)] string displayName,
        SERVICE_ACCESS_RIGHTS desiredAccess,
        SERVICE_TYPE serviceType,
        START_TYPE startType,
        ERROR_CONTROL errorControl,
        [MarshalAs(UnmanagedType.LPWStr)] string binaryPathName,
        [MarshalAs(UnmanagedType.LPWStr)] string loadOrderGroup,
        IntPtr tagId,
        [MarshalAs(UnmanagedType.LPWStr)] string dependencies,
        [MarshalAs(UnmanagedType.LPWStr)] string serviceStartName,
        [MarshalAs(UnmanagedType.LPWStr)] string password);

    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
    public delegate bool StartServiceW(
        IntPtr hService,
        uint numServiceArgs,
        [MarshalAs(UnmanagedType.LPWStr)] string serviceArgVectors);

    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
    public delegate bool DeleteService(IntPtr hService);

    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
    public delegate bool CloseServiceHandle(IntPtr hSCObject);
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate Native.NTSTATUS NtOpenProcess(
        ref IntPtr processHandle,
        PROCESS_ACCESS_FLAGS desiredAccess,
        ref OBJECT_ATTRIBUTES objectAttributes,
        ref CLIENT_ID clientId);
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate Native.NTSTATUS NtAllocateVirtualMemory(
        IntPtr processHandle,
        ref IntPtr baseAddress,
        IntPtr zeroBits,
        ref IntPtr regionSize,
        MEMORY_ALLOCATION allocationType,
        MEMORY_PROTECTION memoryProtection);
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate Native.NTSTATUS NtFreeVirtualMemory(
        IntPtr processHandle,
        ref IntPtr baseAddress,
        ref IntPtr regionSize,
        MEMORY_ALLOCATION freeType);
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate Native.NTSTATUS NtWriteVirtualMemory(
        IntPtr processHandle,
        IntPtr baseAddress,
        IntPtr buffer,
        uint bufferLength,
        ref uint bytesWritten);
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate Native.NTSTATUS NtProtectVirtualMemory(
        IntPtr processHandle,
        ref IntPtr baseAddress,
        ref IntPtr regionSize,
        MEMORY_PROTECTION newProtect,
        ref MEMORY_PROTECTION oldProtect);
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate Native.NTSTATUS NtCreateThreadEx(
        out IntPtr threadHandle,
        Win32.WinNT.ACCESS_MASK desiredAccess,
        IntPtr objectAttributes,
        IntPtr processHandle,
        IntPtr startAddress,
        IntPtr parameter,
        bool createSuspended,
        int stackZeroBits,
        int sizeOfStack,
        int maximumStackSize,
        IntPtr attributeList);
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void RtlZeroMemory(
        IntPtr destination,
        int length);
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate uint NtQueryInformationProcess(
        IntPtr processHandle,
        PROCESS_INFO_CLASS processInformationClass,
        IntPtr processInformation,
        int processInformationLength,
        ref uint returnLength);
}