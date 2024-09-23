using System.Text.Json.Serialization;

namespace TeamServer.Events;

public abstract class SharpC2Event
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    
    [JsonPropertyName("type")]
    public abstract EventType Type { get; }
    
    [JsonPropertyName("date")]
    public DateTime Date { get; set; }
}

public enum EventType
{
    USER_AUTH,
    WEB_LOG
}