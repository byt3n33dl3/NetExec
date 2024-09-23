using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

using TeamServer.Events;

namespace TeamServer.Webhooks;

public sealed class CustomWebhook : SharpC2Webhook
{
    public override WebhookConsumer Consumer
        => WebhookConsumer.CUSTOM;
    
    public override async Task Send(SharpC2Event ev)
    {
        // serialise to json
        var opts = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

        // needs to be cast to the correct type
        var json = ev switch
        {
            UserAuthEvent userAuthEvent => JsonContent.Create(userAuthEvent,
                new MediaTypeWithQualityHeaderValue("application/json"), opts),
            
            WebLogEvent webLogEvent => JsonContent.Create(webLogEvent,
                new MediaTypeWithQualityHeaderValue("application/json"), opts),
            
            _ => throw new ArgumentOutOfRangeException()
        };

        // post
        using var client = new HttpClient();
        await client.PostAsync(Url, json);
    }
}