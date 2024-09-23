﻿using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Drone.Commands;

public sealed class PowerShellImport : DroneCommand
{
    public override byte Command => 0x3E;
    public override bool Threaded => false;

    public static string ImportedScript { get; private set; }
    
    public override Task Execute(DroneTask task, CancellationToken cancellationToken)
    {
        ImportedScript = Encoding.ASCII.GetString(task.Artefact);
        return Task.CompletedTask;
    }
}