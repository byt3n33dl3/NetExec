using TeamServer.Interfaces;
using TeamServer.Storage;
using TeamServer.Webhooks;

namespace TeamServer.Services;

public sealed class WebhookService : IWebhookService
{
    private readonly IDatabaseService _db;
    private readonly List<SharpC2Webhook> _running = new();

    public WebhookService(IDatabaseService db)
    {
        _db = db;
    }

    public async Task LoadWebhooksFromDb()
    {
        var conn = _db.GetAsyncConnection();
        
        var slack = await conn.Table<SlackWebhookDao>().ToArrayAsync();
        var custom = await conn.Table<CustomWebhookDao>().ToArrayAsync();
        
        _running.AddRange(slack.Select(s => (SlackWebhook)s));
        _running.AddRange(custom.Select(c => (CustomWebhook)c));
    }

    public async Task Add(SharpC2Webhook webhook)
    {
        _running.Add(webhook);
        
        var conn = _db.GetAsyncConnection();

        switch (webhook.Consumer)
        {
            case WebhookConsumer.SLACK:
                await conn.InsertAsync((SlackWebhookDao)webhook);
                break;

            case WebhookConsumer.CUSTOM:
                await conn.InsertAsync((CustomWebhookDao)webhook);
                break;
            
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public IEnumerable<T> Get<T>() where T : SharpC2Webhook
    {
        return _running.OfType<T>();
    }

    public T Get<T>(string name) where T : SharpC2Webhook
    {
        var webhook = _running.FirstOrDefault(h => h.Name.Equals(name));
        return webhook as T;
    }


    public async Task Delete(SharpC2Webhook webhook)
    {
        _running.Remove(webhook);
        
        var conn = _db.GetAsyncConnection();

        switch (webhook.Consumer)
        {
            case WebhookConsumer.SLACK:
                await conn.DeleteAsync((SlackWebhookDao)webhook);
                break;
            
            case WebhookConsumer.CUSTOM:
                await conn.DeleteAsync((CustomWebhookDao)webhook);
                break;
            
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}