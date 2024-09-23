using SQLite;

using TeamServer.Events;
using TeamServer.Webhooks;

namespace TeamServer.Storage;

[Table("webhooks_custom")]
public sealed class CustomWebhookDao
{
    [PrimaryKey, Column("id")]
    public string Id { get; set; }

    [Column("name")]
    public string Name { get; set; }

    [Column("event")]
    public int EventType { get; set; }

    [Column("url")]
    public string Url { get; set; }

    public static implicit operator CustomWebhookDao(CustomWebhook webhook)
    {
        return new CustomWebhookDao
        {
            Id = webhook.Id,
            Name = webhook.Name,
            EventType = (int)webhook.EventType,
            Url = webhook.Url
        };
    }

    public static implicit operator CustomWebhook(CustomWebhookDao dao)
    {
        return dao is null
            ? null
            : new CustomWebhook
            {
                Id = dao.Id,
                Name = dao.Name,
                EventType = (EventType)dao.EventType,
                Url = dao.Url
            };
    }
}