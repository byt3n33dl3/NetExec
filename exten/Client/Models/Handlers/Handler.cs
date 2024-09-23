namespace Client.Models.Handlers;

public abstract class Handler
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Profile { get; set; }
    public PayloadType PayloadType { get; set; }

    public override string ToString()
    {
        return Name;
    }
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