using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Drone.Commands;

public class KillProcess : DroneCommand
{
    public override byte Command => 0x1F;
    public override bool Threaded => false;
    
    public override Task Execute(DroneTask task, CancellationToken cancellationToken)
    {
        var pid = task.Arguments["pid"];
        
        using var process = Process.GetProcessById(int.Parse(pid));
        process.Kill();
        
        return Task.CompletedTask;
    }
}