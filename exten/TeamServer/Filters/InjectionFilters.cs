using Microsoft.AspNetCore.Mvc.Filters;

using SharpC2.API.Requests;

using TeamServer.Controllers;
using TeamServer.Handlers;
using TeamServer.Interfaces;

namespace TeamServer.Filters;

public sealed class InjectionFilters : IAsyncActionFilter
{
    private readonly IHandlerService _handlers;
    private readonly IPayloadService _payloads;

    public InjectionFilters(IHandlerService handlers, IPayloadService payloads)
    {
        _handlers = handlers;
        _payloads = payloads;
    }
    
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.Controller is TasksController)
        {
            if (context.ActionArguments.TryGetValue("request", out var action))
            {
                if (action is TaskRequest taskRequest)
                {
                    // shspawn
                    if (taskRequest.Command == 0x4B)
                    {
                        var handler = _handlers.Get<Handler>(taskRequest.Arguments["handler"]);

                        if (handler is null)
                            throw new ArgumentException("Handler not found");

                        // generate shellcode
                        taskRequest.Artefact = await _payloads.GeneratePayload(handler, PayloadFormat.SHELLCODE);
                    }
                }
            }
        }

        // go to next
        await next();
    }
}