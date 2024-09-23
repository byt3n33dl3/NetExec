using System.Text.Json.Serialization;
using SharpC2.API.Responses;

namespace TeamServer.Events;

public sealed class UserAuthEvent : SharpC2Event
{
    [JsonIgnore]
    public override EventType Type
        => EventType.USER_AUTH;

    [JsonPropertyName("nick")]
    public string Nick { get; set; }
    
    [JsonPropertyName("result")]
    public bool Result { get; set; }

    public static implicit operator UserAuthEventResponse(UserAuthEvent ev)
    {
        return new UserAuthEventResponse
        {
            Id = ev.Id,
            Date = ev.Date,
            Nick = ev.Nick,
            Result = ev.Result
        };
    }
}