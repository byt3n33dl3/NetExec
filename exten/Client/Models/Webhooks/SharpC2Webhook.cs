using Client.Models.Events;
using SharpC2.API.Responses;

namespace Client.Models.Webhooks;

public sealed class SharpC2Webhook
{
    public string Id { get; set; }
    public string Name { get; set; }
    public WebhookConsumer Consumer { get; set; }
    public EventType Event { get; set; }
    public string Url { get; set; }

    public static implicit operator SharpC2Webhook(WebhookResponse response)
    {
        return response is null
            ? null
            : new SharpC2Webhook
            {
                Id = response.Id,
                Name = response.Name,
                Consumer = (WebhookConsumer)response.Consumer,
                Event = (EventType)response.Event,
                Url = response.Url
            };
    }
}

public enum WebhookConsumer
{
    SLACK,
    CUSTOM
}