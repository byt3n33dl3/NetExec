using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

using SharpC2.API;
using SharpC2.API.Requests;
using SharpC2.API.Responses;

using TeamServer.Events;
using TeamServer.Hubs;
using TeamServer.Interfaces;
using TeamServer.Utilities;
using TeamServer.Webhooks;

using EventType = TeamServer.Events.EventType;

namespace TeamServer.Controllers;

[Authorize]
[ApiController]
[Route(Routes.V1.Webhooks)]
public sealed class WebhooksController : ControllerBase
{
    private readonly IWebhookService _webhooks;
    private readonly IEventService _events;
    private readonly IHubContext<NotificationHub, INotificationHub> _hub;

    public WebhooksController(IWebhookService webhooks, IEventService events, IHubContext<NotificationHub, INotificationHub> hub)
    {
        _webhooks = webhooks;
        _events = events;
        _hub = hub;
    }

    [HttpGet]
    public ActionResult<IEnumerable<WebhookResponse>> GetWebhooks()
    {
        var webhooks = _webhooks.Get<SharpC2Webhook>();
        var response = webhooks.Select(h => (WebhookResponse)h);

        return Ok(response);
    }

    [HttpGet("{name}")]
    public ActionResult<WebhookResponse> GetWebhook(string name)
    {
        var webhook = _webhooks.Get<SharpC2Webhook>(name);

        if (webhook is null)
            return NotFound();

        return Ok((WebhookResponse)webhook);
    }

    [HttpPost]
    public async Task<ActionResult<WebhookResponse>> CreateWebhook(WebhookRequest request)
    {
        // check if name is taken
        var existing = _webhooks.Get<SharpC2Webhook>(request.Name);
        
        if (existing is not null)
            return BadRequest($"Name \"{request.Name}\" already in use.");

        // create webhook class
        SharpC2Webhook webhook = (WebhookConsumer)request.Consumer switch
        {
            WebhookConsumer.SLACK => new SlackWebhook(),
            WebhookConsumer.CUSTOM => new CustomWebhook(),
            
            _ => throw new ArgumentOutOfRangeException()
        };

        // set properties
        webhook.Id = Helpers.GenerateShortGuid();
        webhook.Name = request.Name;
        webhook.EventType = (EventType)request.Event;
        webhook.Url = request.Url;
        
        // add webhook
        await _webhooks.Add(webhook);
        
        // register callback event
        Task Callback(SharpC2Event ev) => webhook.Send(ev);
        _events.SubscribeEvent(webhook.Id, Callback);
        
        // tell hub
        await _hub.Clients.All.WebhookCreated(webhook.Name);

        // return response
        return Ok((WebhookResponse)webhook);
    }

    [HttpDelete("{name}")]
    public async Task<IActionResult> DeleteWebhook(string name)
    {
        var webhook = _webhooks.Get<SharpC2Webhook>(name);

        if (webhook is null)
            return NotFound();

        _events.UnsubscribeEvent(webhook.Id);

        await _webhooks.Delete(webhook);
        await _hub.Clients.All.WebhookDeleted(webhook.Name);
        
        return Ok();
    }
}