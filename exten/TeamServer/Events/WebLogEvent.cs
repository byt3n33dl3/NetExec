using System.Text.Json.Serialization;

using SharpC2.API.Responses;

namespace TeamServer.Events;

public sealed class WebLogEvent : SharpC2Event
{
    [JsonIgnore]
    public override EventType Type
        => EventType.WEB_LOG;

    [JsonPropertyName("method")]
    public string Method { get; set; }
    
    [JsonPropertyName("uri")]
    public string Uri { get; set; }
    
    [JsonPropertyName("user_agent")]
    public string UserAgent { get; set; }
    
    [JsonPropertyName("source_address")]
    public string SourceAddress { get; set; }
    
    [JsonPropertyName("response_code")]
    public int ResponseCode { get; set; }

    public static implicit operator WebLogEventResponse(WebLogEvent ev)
    {
        return new WebLogEventResponse
        {
            Id = ev.Id,
            Date = ev.Date,
            Method = ev.Method,
            Uri = ev.Uri,
            UserAgent = ev.UserAgent,
            SourceAddress = ev.SourceAddress,
            ResponseCode = ev.ResponseCode
        };
    }
}