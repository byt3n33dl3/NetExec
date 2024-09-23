using System.Threading;
using System.Threading.Tasks;

using Drone.CommModules;

namespace Drone.Commands;

public sealed class Connect : DroneCommand
{
    public override byte Command => 90;
    public override bool Threaded => false;
    
    public override async Task Execute(DroneTask task, CancellationToken cancellationToken)
    {
        var address = task.Arguments["target"];
        var port = int.Parse(task.Arguments["port"]);
        var commModule = new TcpCommModule(address, port);
        
        await Drone.AddChildCommModule(task.Id, commModule);
    }
}