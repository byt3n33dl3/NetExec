using TeamServer.Events;

namespace TeamServer.Interfaces;

public interface IEventService
{
    Task Add(SharpC2Event ev);

    Task<IEnumerable<UserAuthEvent>> GetAuthEvents();
    Task<UserAuthEvent> GetAuthEvent(string id);

    Task<IEnumerable<WebLogEvent>> GetWebLogEvents();
    Task<WebLogEvent> GetWebLogEvent(string id);

    void SubscribeEvent(string id, Func<SharpC2Event, Task> callback);
    //Func<SharpC2Event, Task> GetCallback(string id);
    void UnsubscribeEvent(string id);
}