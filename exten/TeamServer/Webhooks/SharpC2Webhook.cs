using SharpC2.API.Responses;
using TeamServer.Events;

namespace TeamServer.Webhooks;

public abstract class SharpC2Webhook
{
    public abstract WebhookConsumer Consumer { get; }

    public string Id { get; set; }
    public string Name { get; set; }
    public EventType EventType { get; set; }
    public string Url { get; set; }

    public abstract Task Send(SharpC2Event ev);

    public static implicit operator WebhookResponse(SharpC2Webhook webhook)
    {
        return new WebhookResponse
        {
            Id = webhook.Id,
            Name = webhook.Name,
            Consumer = (int)webhook.Consumer,
            Event = (int)webhook.EventType,
            Url = webhook.Url
        };
    }
}

public enum WebhookConsumer
{
    SLACK,
    CUSTOM
}