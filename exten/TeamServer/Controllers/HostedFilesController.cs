﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

using SharpC2.API;
using SharpC2.API.Requests;
using SharpC2.API.Responses;

using TeamServer.Handlers;
using TeamServer.Hubs;
using TeamServer.Interfaces;

namespace TeamServer.Controllers;

[Authorize]
[ApiController]
[Route(Routes.V1.HostedFiles)]
public class HostedFilesController : ControllerBase
{
    private readonly IHandlerService _handlers;
    private readonly IHostedFilesService _hostedFiles;
    private readonly IHubContext<NotificationHub, INotificationHub> _hub;

    public HostedFilesController(IHandlerService handlers, IHostedFilesService hostedFiles, IHubContext<NotificationHub, INotificationHub> hub)
    {
        _handlers = handlers;
        _hostedFiles = hostedFiles;
        _hub = hub;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<HostedFileResponse>>> GetHostedFiles()
    {
        var files = await _hostedFiles.Get();
        var response = files.Select(f => (HostedFileResponse)f);

        return Ok(response);
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<HostedFileResponse>> GetHostedFile(string id)
    {
        var file = await _hostedFiles.Get(id);

        if (file is null)
            return NotFound();
        
        return Ok((HostedFileResponse)file);
    }

    [HttpPost]
    public async Task<IActionResult> HostFile([FromBody] HostedFileRequest request)
    {
        var handler = _handlers.Get<HttpHandler>(request.Handler);

        if (handler is null)
            return NotFound();

        var fileName = request.Uri.Split('/').Last();
        var fullPath = Path.Combine(handler.DataDirectory, fileName);
        await System.IO.File.WriteAllBytesAsync(fullPath, request.Bytes);

        var hostedFile = (HostedFile)request;

        await _hostedFiles.Add(hostedFile);
        await _hub.Clients.All.HostedFileAdded(hostedFile.Id);

        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFile(string id)
    {
        var hostedFile = await _hostedFiles.Get(id);

        if (hostedFile is null)
            return NotFound("Hosted file not found");

        var handler = _handlers.Get<HttpHandler>(hostedFile.Handler);

        if (handler is null)
            return NotFound("Handler not found");
        
        var fileName = hostedFile.Uri.Split('/').Last();
        var fullPath = Path.Combine(handler.DataDirectory, fileName);
        
        if (System.IO.File.Exists(fullPath))
            System.IO.File.Delete(fullPath);

        await _hostedFiles.Delete(hostedFile);
        await _hub.Clients.All.HostedFileDeleted(hostedFile.Id);
        
        return NoContent();
    }
}