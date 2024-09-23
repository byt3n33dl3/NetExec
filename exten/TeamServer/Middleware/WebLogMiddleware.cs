using Microsoft.AspNetCore.SignalR;

using TeamServer.Events;
using TeamServer.Hubs;
using TeamServer.Interfaces;
using TeamServer.Utilities;

namespace TeamServer.Middleware;

public class WebLogMiddleware
{
    private readonly IConfiguration _configuration;
    private readonly RequestDelegate _next;

    private readonly IProfileService _profiles;
    private readonly IEventService _events;
    private readonly IHubContext<NotificationHub, INotificationHub> _hub;

    public WebLogMiddleware(RequestDelegate next, IConfiguration configuration, IProfileService profiles, IEventService events, IHubContext<NotificationHub, INotificationHub> hub)
    {
        _next = next;
        _configuration = configuration;
        _profiles = profiles;
        _events = events;
        _hub = hub;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        // allow the request pipeline to complete
        await _next(context);

        // if the response is not 200 or 204
        if (context.Response.StatusCode != 200 && context.Response.StatusCode != 204)
        {
            // get the c2 profile
            var profileName = _configuration.GetValue<string>("profile");
            var profile = await _profiles.Get(profileName);

            if (profile is not null)
            {
                var log = false;
                var path = context.Request.Path.ToUriComponent();

                // if it was a GET request check the GetPaths in the c2 profile
                if (context.Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
                    if (!profile.Http.GetPaths.Contains(path))
                        log = true;

                // if it was a POST request check the PostPaths in the c2 profile
                if (context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
                    if (!profile.Http.PostPaths.Contains(path))
                        log = true;

                if (log)
                {
                    var ev = new WebLogEvent
                    {
                        Id = Helpers.GenerateShortGuid(),
                        Date = DateTime.UtcNow,
                        Method = context.Request.Method,
                        Uri = context.Request.Path.ToUriComponent(),
                        UserAgent = context.Request.Headers.UserAgent.ToString(),
                        SourceAddress = context.Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        ResponseCode = context.Response.StatusCode
                    };

                    await _events.Add(ev);
                    await _hub.Clients.All.NewEvent(EventType.WEB_LOG, ev.Id);
                }
            }
        }
    }
}

public static class WebLogMiddlewareExtensions
{
    public static IApplicationBuilder UseWebLogMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<WebLogMiddleware>();
    }
}