using System;
using System.Runtime.InteropServices;
using System.Text;

using DInvoke.DynamicInvoke;

using Native = DInvoke.Data.Native;
using Win32 = DInvoke.Data.Win32;

namespace Drone.Interop;

using static Data;
using static Delegates;

public static class Methods
{
    public static bool CreateProcess(string applicationName, string commandLine,
        ref SECURITY_ATTRIBUTES processAttributes, ref SECURITY_ATTRIBUTES threadAttributes,
        bool inheritHandles, PROCESS_CREATION_FLAGS creationFlags, IntPtr environment, string currentDirectory,
        STARTUPINFOW startupInfo, out PROCESS_INFORMATION processInformation)
    {
        object[] parameters =
        {
            applicationName, commandLine, processAttributes, threadAttributes, inheritHandles, creationFlags,
            environment, currentDirectory, startupInfo, new PROCESS_INFORMATION()
        };

        var result = (bool)Generic.DynamicApiInvoke(
            "kernel32.dll",
            "CreateProcessW",
            typeof(CreateProcessW),
            ref parameters);
        
        processInformation = (PROCESS_INFORMATION)parameters[9];
        return result;
    }

    public static uint QueueUserApc(IntPtr fnApc, IntPtr hThread)
    {
        object[] parameters = { fnApc, hThread, IntPtr.Zero };
        
        return (uint)Generic.DynamicApiInvoke(
            "kernel32.dll",
            "QueueUserAPC",
            typeof(QueueUserApc),
            ref parameters);
    }

    public static uint ResumeThread(IntPtr hThread)
    {
        object[] parameters = { hThread };
        
        return (uint)Generic.DynamicApiInvoke(
            "kernel32.dll",
            "ResumeThread",
            typeof(ResumeThread),
            ref parameters);
    }
    
    public static IntPtr OpenProcessToken(IntPtr hProcess, TOKEN_ACCESS tokenAccess)
    {
        var hToken = IntPtr.Zero;
        object[] parameters = { hProcess, tokenAccess, hToken };

        Generic.DynamicApiInvoke(
            "advapi32.dll",
            "OpenProcessToken",
            typeof(OpenProcessToken),
            ref parameters);

        hToken = (IntPtr)parameters[2];
        return hToken;
    }
    
    public static IntPtr LogonUserW(string username, string domain, string password, LOGON_USER_TYPE logonType,
        LOGON_USER_PROVIDER logonProvider)
    {
        var hToken = IntPtr.Zero;
        object[] parameters = { username, domain, password, logonType,logonProvider, hToken };
        
        Generic.DynamicApiInvoke(
            "advapi32.dll",
            "LogonUserW",
            typeof(LogonUserW),
            ref parameters);
        
        hToken = (IntPtr)parameters[5];
        return hToken;
    }
    
    public static bool ImpersonateToken(IntPtr hToken)
    {
        object[] parameters = { hToken };
        
        return (bool)Generic.DynamicApiInvoke(
            "advapi32.dll",
            "ImpersonateLoggedOnUser",
            typeof(ImpersonateLoggedOnUser),
            ref parameters);
    }
    
    public static IntPtr DuplicateTokenEx(IntPtr hExistingToken, DInvoke.Data.Win32.WinNT.ACCESS_MASK tokenAccess,
        SECURITY_IMPERSONATION_LEVEL impersonationLevel, TOKEN_TYPE tokenType)
    {
        var hNewToken = IntPtr.Zero;
        
        var lpTokenAttributes = new SECURITY_ATTRIBUTES();
        lpTokenAttributes.nLength = Marshal.SizeOf(lpTokenAttributes);

        object[] parameters =
        {
            hExistingToken, (uint)tokenAccess, lpTokenAttributes, impersonationLevel,
            tokenType, hNewToken
        };

        Generic.DynamicApiInvoke(
            "advapi32.dll",
            "DuplicateTokenEx",
            typeof(DuplicateTokenEx),
            ref parameters);

        hNewToken = (IntPtr)parameters[5];
        return hNewToken;
    }

    public static bool RevertToSelf()
    {
        object[] parameters = { };

        return (bool) Generic.DynamicApiInvoke(
            "advapi32.dll",
            "RevertToSelf",
            typeof(RevertToSelf),
            ref parameters);
    }

    public static void CloseHandle(IntPtr hObject)
    {
        object[] parameters = { hObject };
        
        Generic.DynamicApiInvoke(
            "kernel32.dll",
            "CloseHandle",
            typeof(CloseHandle),
            ref parameters);
    }

    public static IntPtr GetStandardHandle(int stdHandle)
    {
        object[] parameters = { stdHandle };
        
        return (IntPtr)Generic.DynamicApiInvoke(
            "kernel32.dll",
            "GetStdHandle",
            typeof(GetStdHandle),
            ref parameters);
    }

    public static bool SetStandardHandle(int stdHandle, IntPtr hHandle)
    {
        object[] parameters = { stdHandle, hHandle };
        
        return (bool)Generic.DynamicApiInvoke(
            "kernel32.dll",
            "SetStdHandle",
            typeof(SetStdHandle),
            ref parameters);
    }

    public static IntPtr GetCommandLine()
    {
        object[] parameters = { };
        
        return (IntPtr)Generic.DynamicApiInvoke(
            "kernel32.dll",
            "GetCommandLineW",
            typeof(GetCommandLineW),
            ref parameters);
    }

    public static bool ReadFile(IntPtr hFile, byte[] lpBuffer, uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, IntPtr lpOverlapped)
    {
        object[] parameters = { hFile, lpBuffer, nNumberOfBytesToRead, (uint)0, lpOverlapped };
        
        var result = (bool)Generic.DynamicApiInvoke(
            "kernel32.dll",
            "ReadFile",
            typeof(ReadFile),
            ref parameters);

        lpNumberOfBytesRead = (uint)parameters[3];
        return result;
    }

    public static bool CreatePipe(out IntPtr hReadPipe, out IntPtr hWritePipe, ref SECURITY_ATTRIBUTES lpPipeAttributes, uint nSize)
    {
        object[] parameters = { IntPtr.Zero, IntPtr.Zero, lpPipeAttributes, nSize };
        
        var result = (bool)Generic.DynamicApiInvoke(
            "kernel32.dll",
            "CreatePipe",
            typeof(CreatePipe),
            ref parameters);

        hReadPipe = (IntPtr)parameters[0];
        hWritePipe = (IntPtr)parameters[1];
        
        return result;
    }

    public static bool PeekNamedPipe(IntPtr hPipe)
    {
        object[] parameters = { hPipe, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, (uint)0, IntPtr.Zero };
        
        return (bool)Generic.DynamicApiInvoke(
            "kernel32.dll",
            "PeekNamedPipe",
            typeof(PeekNamedPipe),
            ref parameters);
    }

    public static bool FreeLibrary(IntPtr hModule)
    {
        object[] parameters = { hModule };
        
        return (bool)Generic.DynamicApiInvoke(
            "kernel32.dll",
            "FreeLibrary",
            typeof(FreeLibrary),
            ref parameters);
    }

    public static bool WriteFile(IntPtr hFile, byte[] lpBuffer, uint nNumberOfBytesToWrite,
        out uint lpNumberOfBytesWritten, IntPtr lpOverlapped)
    {
        object[] parameters =
        {
            hFile, lpBuffer, nNumberOfBytesToWrite, (uint)0, lpOverlapped
        };
        
        var result = (bool)Generic.DynamicApiInvoke(
            "kernel32.dll",
            "WriteFile",
            typeof(WriteFile),
            ref parameters);

        lpNumberOfBytesWritten = (uint)parameters[3];
        return result;
    }

    public static IntPtr CreateTransaction(IntPtr lpTransactionAttributes, IntPtr uow, int createOptions, int isolationLevel,
        int isolationFlags, int timeout, StringBuilder description)
    {
        object[] parameters =
        {
            lpTransactionAttributes, uow, createOptions, isolationLevel,
            isolationFlags, timeout, description
        };
        
        return (IntPtr)Generic.DynamicApiInvoke(
            "ktmw32.dll",
            "CreateTransaction",
            typeof(CreateTransaction),
            ref parameters,
            true);
    }

    public static IntPtr CreateFileTransacted(string lpFileName, uint dwDesiredAccess,
        uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes,
        IntPtr hTemplateFile, IntPtr hTransaction, ref ushort pusMiniVersion, IntPtr nullValue)
    {
        object[] parameters =
        {
            lpFileName, dwDesiredAccess, dwShareMode, lpSecurityAttributes, dwCreationDisposition,
            dwFlagsAndAttributes, hTemplateFile, hTransaction, pusMiniVersion, nullValue
        };
        
        return (IntPtr)Generic.DynamicApiInvoke(
            "kernel32.dll",
            "CreateFileTransactedW",
            typeof(CreateFileTransactedW),
            ref parameters);
    }

    public static void WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds)
    {
        object[] parameters = { hHandle, dwMilliseconds };
        
        Generic.DynamicApiInvoke(
            "kernel32.dll",
            "WaitForSingleObject",
            typeof(WaitForSingleObject),
            ref parameters);
    }

    public static IntPtr OpenSCManager(string machineName, SCM_ACCESS_RIGHTS desiredAccess)
    {
        object[] parameters = { machineName, null, desiredAccess };
        
        return (IntPtr)Generic.DynamicApiInvoke(
            "advapi32.dll",
            "OpenSCManagerW",
            typeof(OpenSCManagerW),
            ref parameters);
    }

    public static IntPtr CreateService(IntPtr hSCManager, string serviceName, string displayName,
        SERVICE_ACCESS_RIGHTS desiredAccess, SERVICE_TYPE serviceType, START_TYPE startType, string binaryPathName)
    {
        object[] parameters =
        {
            hSCManager, serviceName, displayName, desiredAccess, serviceType, startType,
            ERROR_CONTROL.SERVICE_ERROR_IGNORE, binaryPathName, null, IntPtr.Zero, null,
            "NT AUTHORITY\\SYSTEM", null
        };
        
        return (IntPtr)Generic.DynamicApiInvoke(
            "advapi32.dll",
            "CreateServiceW",
            typeof(CreateServiceW),
            ref parameters);
    }

    public static bool StartService(IntPtr hService)
    {
        object[] parameters = { hService, (uint)0, null };
        
        return (bool)Generic.DynamicApiInvoke(
            "advapi32.dll",
            "StartServiceW",
            typeof(StartServiceW),
            ref parameters);
    }

    public static bool DeleteService(IntPtr hService)
    {
        object[] parameters = { hService };
        
        return (bool)Generic.DynamicApiInvoke(
            "advapi32.dll",
            "DeleteService",
            typeof(DeleteService),
            ref parameters);
    }

    public static bool CloseServiceHandle(IntPtr hSCObject)
    {
        object[] parameters = { hSCObject };
        
        return (bool)Generic.DynamicApiInvoke(
            "advapi32.dll",
            "CloseServiceHandle",
            typeof(CloseServiceHandle),
            ref parameters);
    }

    public static bool TerminateProcess(IntPtr hProcess, uint exitCode)
    {
        object[] parameters = { hProcess, exitCode };
        
        return (bool)Generic.DynamicApiInvoke(
            "kernel32.dll",
            "TerminateProcess",
            typeof(TerminateProcess),
            ref parameters);
    }
    
    public static Native.NTSTATUS NtOpenProcess(uint pid, uint desiredAccess, ref IntPtr hProcess)
    {
        var oa = new OBJECT_ATTRIBUTES();
        var ci = new CLIENT_ID { UniqueProcess = (IntPtr)pid };

        object[] parameters = { hProcess, desiredAccess, oa, ci };

        var status = (Native.NTSTATUS)Generic.DynamicApiInvoke(
            "ntdll.dll",
            "NtOpenProcess",
            typeof(NtOpenProcess),
            ref parameters);

        hProcess = (IntPtr)parameters[0];
        return status;
    }

    public static Native.NTSTATUS NtAllocateVirtualMemory(IntPtr hProcess, int length, MEMORY_ALLOCATION memoryAllocation, MEMORY_PROTECTION protection, ref IntPtr baseAddress)
    {
        var regionSize = new IntPtr(length);

        object[] parameters =
        {
            hProcess, baseAddress, IntPtr.Zero, regionSize,
            memoryAllocation,
            protection
        };

        var status = (Native.NTSTATUS) Generic.DynamicApiInvoke(
            "ntdll.dll",
            "NtAllocateVirtualMemory",
            typeof(NtAllocateVirtualMemory),
            ref parameters);

        baseAddress = (IntPtr)parameters[1];
        return status;
    }

    public static Native.NTSTATUS NtFreeVirtualMemory(IntPtr hProcess, IntPtr baseAddress, int size)
    {
        var regionSize = new IntPtr(size);
        
        object[] parameters =
        {
            hProcess, baseAddress, regionSize,
            MEMORY_ALLOCATION.MEM_RELEASE
        };

        return (Native.NTSTATUS)Generic.DynamicApiInvoke(
            "ntdll.dll",
            "NtFreeVirtualMemory",
            typeof(NtFreeVirtualMemory),
            ref parameters);
    }

    public static Native.NTSTATUS NtWriteVirtualMemory(IntPtr hProcess, IntPtr baseAddress, byte[] data)
    {
        var buf = Marshal.AllocHGlobal(data.Length);
        Marshal.Copy(data, 0, buf, data.Length);
        
        object[] parameters =
        {
            hProcess, baseAddress, buf, (uint)data.Length, (uint)0
        };

        var status = (Native.NTSTATUS)Generic.DynamicApiInvoke(
            "ntdll.dll",
            "NtWriteVirtualMemory",
            typeof(NtWriteVirtualMemory),
            ref parameters);

        Marshal.FreeHGlobal(buf);
        return status;
    }

    public static Native.NTSTATUS NtProtectVirtualMemory(IntPtr hProcess, IntPtr baseAddress, int length,
        MEMORY_PROTECTION protection, out MEMORY_PROTECTION oldProtection)
    {
        var regionSize = new IntPtr(length);

        object[] parameters =
        {
            hProcess, baseAddress, regionSize,
            protection, (MEMORY_PROTECTION)0
        };

        var status = (Native.NTSTATUS)Generic.DynamicApiInvoke(
            "ntdll.dll",
            "NtProtectVirtualMemory",
            typeof(NtProtectVirtualMemory),
            ref parameters);

        oldProtection = (MEMORY_PROTECTION)parameters[4];
        return status;
    }

    public static Native.NTSTATUS NtCreateThreadEx(IntPtr hProcess, IntPtr baseAddress, ref IntPtr hThread)
    {
        const Win32.WinNT.ACCESS_MASK access =
            Win32.WinNT.ACCESS_MASK.SPECIFIC_RIGHTS_ALL | Win32.WinNT.ACCESS_MASK.STANDARD_RIGHTS_ALL;

        object[] parameters =
        {
            hThread, access, IntPtr.Zero, hProcess, baseAddress,
            IntPtr.Zero, false, 0, 0, 0, IntPtr.Zero
        };

        var status = (Native.NTSTATUS)Generic.DynamicApiInvoke(
            "ntdll.dll",
            "NtCreateThreadEx",
            typeof(NtCreateThreadEx),
            ref parameters);

        hThread = (IntPtr)parameters[0];
        return status;
    }

    public static bool NtQueryInformationProcessWow64Information(IntPtr hProcess)
    {
        var result = NtQueryInformationProcess(
            hProcess,
            PROCESS_INFO_CLASS.ProcessWow64Information,
            out var pProcInfo);

        if (result != 0)
            throw new UnauthorizedAccessException("Access is denied.");

        return Marshal.ReadIntPtr(pProcInfo) != IntPtr.Zero;
    }

    public static PROCESS_BASIC_INFORMATION QueryProcessBasicInformation(IntPtr hProcess)
    {
        NtQueryInformationProcess(
            hProcess,
            PROCESS_INFO_CLASS.ProcessBasicInformation,
            out var pProcInfo);

        return (PROCESS_BASIC_INFORMATION)Marshal.PtrToStructure(pProcInfo, typeof(PROCESS_BASIC_INFORMATION));
    }
    
    public static Native.NTSTATUS NtQueryInformationProcess(IntPtr hProcess, PROCESS_INFO_CLASS processInfoClass, out IntPtr pProcInfo)
    {
        int processInformationLength;
        uint retLen = 0;

        switch (processInfoClass)
        {
            case PROCESS_INFO_CLASS.ProcessWow64Information:
            {
                pProcInfo = Marshal.AllocHGlobal(IntPtr.Size);
                RtlZeroMemory(pProcInfo, IntPtr.Size);
                processInformationLength = IntPtr.Size;
                
                break;
            }

            case PROCESS_INFO_CLASS.ProcessBasicInformation:
            {
                var pbi = new PROCESS_BASIC_INFORMATION();
                pProcInfo = Marshal.AllocHGlobal(Marshal.SizeOf(pbi));
                RtlZeroMemory(pProcInfo, Marshal.SizeOf(pbi));
                Marshal.StructureToPtr(pbi, pProcInfo, true);
                processInformationLength = Marshal.SizeOf(pbi);
                
                break;
            }

            default:
                throw new InvalidOperationException($"Invalid ProcessInfoClass: {processInfoClass}");
        }

        object[] parameters = { hProcess, processInfoClass, pProcInfo, processInformationLength, retLen };

        var status = (Native.NTSTATUS)Generic.DynamicApiInvoke(
            "ntdll.dll",
            "NtQueryInformationProcess",
            typeof(NtQueryInformationProcess),
            ref parameters);

        pProcInfo = (IntPtr)parameters[2];
        return status;
    }
    
    public static void RtlZeroMemory(IntPtr destination, int length)
    {
        object[] parameters = { destination, length };

        Generic.DynamicApiInvoke(
            "ntdll.dll",
            "RtlZeroMemory",
            typeof(RtlZeroMemory),
            ref parameters);
    }
}