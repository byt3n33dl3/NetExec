using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Drone.Commands;

public sealed class ChangeDirectory : DroneCommand
{
    public override byte Command => 0x15;
    public override bool Threaded => false;

    public override Task Execute(DroneTask task, CancellationToken cancellationToken)
    {
        if (!task.Arguments.TryGetValue("path", out var path))
            path = Environment.GetFolderPath(Environment.SpecialFolder.Personal);

        Directory.SetCurrentDirectory(path);
        return Task.CompletedTask;
    }
}