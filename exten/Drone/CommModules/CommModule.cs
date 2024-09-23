using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Drone.CommModules;

public abstract class CommModule
{
    public abstract ModuleType Type { get; }
    public abstract ModuleMode Mode { get; }
    
    public abstract void Init(Metadata metadata);
    public abstract Task SendFrame(C2Frame frame);

    public enum ModuleType
    {
        EGRESS,
        P2P
    }

    public enum ModuleMode
    {
        SERVER,
        CLIENT
    }
}

public abstract class EgressCommModule : CommModule
{
    public abstract Task<IEnumerable<C2Frame>> CheckIn();
}

public abstract class P2PCommModule : CommModule
{
    public abstract bool Running { get; protected set; }
    
    public abstract event Func<C2Frame, Task> FrameReceived;
    public abstract event Action OnException; 
    
    public abstract Task Start();
    public abstract Task Run();
    public abstract void Stop();
}