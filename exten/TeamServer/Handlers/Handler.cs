using TeamServer.C2Profiles;

namespace TeamServer.Handlers;

public abstract class Handler
{
    public string Id { get; set; }
    public string Name { get; set; }
    public C2Profile C2Profile { get; set; }
    public PayloadType PayloadType { get; set; }
    public abstract HandlerType HandlerType { get; }
}

public enum HandlerType
{
    HTTP,
    SMB,
    TCP,
    EXTERNAL
}

public enum PayloadType
{
    HTTP,
    HTTPS,
    BIND_PIPE,
    BIND_TCP,
    REVERSE_TCP,
    EXTERNAL
}