using SQLite;

using TeamServer.Events;
using TeamServer.Webhooks;

namespace TeamServer.Storage;

[Table("webhooks_slack")]
public sealed class SlackWebhookDao
{
    [PrimaryKey, Column("id")]
    public string Id { get; set; }

    [Column("name")]
    public string Name { get; set; }

    [Column("event")]
    public int EventType { get; set; }

    [Column("url")]
    public string Url { get; set; }

    public static implicit operator SlackWebhookDao(SlackWebhook webhook)
    {
        return new SlackWebhookDao
        {
            Id = webhook.Id,
            Name = webhook.Name,
            EventType = (int)webhook.EventType,
            Url = webhook.Url
        };
    }

    public static implicit operator SlackWebhook(SlackWebhookDao dao)
    {
        return dao is null
            ? null
            : new SlackWebhook
            {
                Id = dao.Id,
                Name = dao.Name,
                EventType = (EventType)dao.EventType,
                Url = dao.Url
            };
    }
}