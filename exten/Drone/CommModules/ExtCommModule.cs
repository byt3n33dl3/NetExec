using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Drone.CommModules;

public class ExtCommModule : P2PCommModule
{
    public override ModuleType Type => ModuleType.P2P;
    public override ModuleMode Mode { get; }

    public override bool Running { get; protected set; }
    
    public override event Func<C2Frame, Task> FrameReceived;
    public override event Action OnException;

    private readonly CancellationTokenSource _tokenSource = new();
    
    // this will be queued by the client controller via reflection
    public static ConcurrentQueue<byte[]> Inbound { get; set; } = new();
    
    // this will be dequeued by the client controller via reflection
    public static ConcurrentQueue<byte[]> Outbound { get; set; } = new();
    
    public override void Init(Metadata metadata)
    {
        // nothing
    }

    public override Task SendFrame(C2Frame frame)
    {
        Outbound.Enqueue(frame.Serialize());
        return Task.CompletedTask;
    }

    public override Task Start()
    {
        Running = true;
        return Task.CompletedTask;
    }

    public override async Task Run()
    {
        while (!_tokenSource.IsCancellationRequested)
        {
            while (Inbound.TryDequeue(out var data))
            {
                try
                {
                    var frame = data.Deserialize<C2Frame>();
                    FrameReceived?.Invoke(frame);
                }
                catch
                {
                    OnException?.Invoke();
                    return;
                }
            }
            
            await Task.Delay(100);
        }
        
        _tokenSource.Dispose();
    }

    public override void Stop()
    {
        Running = false;
        _tokenSource.Cancel();
    }
}