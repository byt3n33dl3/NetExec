using TeamServer.Webhooks;

namespace TeamServer.Interfaces;

public interface IWebhookService
{
    // create
    Task Add(SharpC2Webhook webhook);
    
    // read
    IEnumerable<T> Get<T>() where T : SharpC2Webhook;
    T Get<T>(string name) where T : SharpC2Webhook;
    
    // delete
    Task Delete(SharpC2Webhook webhook);
}