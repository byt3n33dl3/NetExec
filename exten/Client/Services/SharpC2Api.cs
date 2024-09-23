﻿using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

using Client.Models.Drones;
using Client.Models.Events;
using Client.Models.Handlers;
using Client.Models.Pivots;
using Client.Models.Tasks;
using Client.Models.Webhooks;

using RestSharp;
using RestSharp.Authenticators;

using SharpC2.API.Requests;
using SharpC2.API.Responses;

namespace Client.Services;

public class SharpC2Api
{
    private RestClient _client;

    public static string AcceptedThumbprint { get; set; }

    public Action<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors> SslException { get; set; }

    public async Task Initialise(string server)
    {
        _client?.Dispose();
        
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = ServerCertificateCustomValidationCallback;
        
        _client = new RestClient(new HttpClient(handler));
        _client.Options.BaseUrl = new Uri($"https://{server}:50050");
        
        // just something to grab the ssl cert
        var request = new RestRequest($"{SharpC2.API.Routes.V1.Authentication}");
        await _client.ExecuteAsync(request);
    }

    public void SetAcceptedThumbprint(string thumbprint)
    {
        AcceptedThumbprint = thumbprint;
    }

    public async Task<AuthenticationResponse> Authenticate(AuthenticationRequest auth)
    {
        var request = new RestRequest($"{SharpC2.API.Routes.V1.Authentication}", Method.Post);
        request.AddParameter("application/json", JsonSerializer.Serialize(auth), ParameterType.RequestBody);
        
        var response = await _client.ExecuteAsync<AuthenticationResponse>(request);

        if (response.Data is not null)
            if (response.Data.Success)
                _client.Authenticator = new JwtAuthenticator(response.Data.Token);

        return response.Data;
    }

    private bool ServerCertificateCustomValidationCallback(HttpRequestMessage msg, X509Certificate2 cert, X509Chain chain, SslPolicyErrors errs)
    {
        if (!string.IsNullOrWhiteSpace(AcceptedThumbprint))
            return AcceptedThumbprint.Equals(cert.Thumbprint);
        
        SslException?.Invoke(msg, cert, chain, errs);
        return false;
    }

    public async Task<IEnumerable<C2Profile>> GetC2Profiles()
    {
        var request = new RestRequest($"{SharpC2.API.Routes.V1.Profiles}");
        var response = await _client.ExecuteAsync<IEnumerable<C2ProfileResponse>>(request);

        return response.Data.Select(p => (C2Profile)p);
    }

    public async Task<IEnumerable<HttpHandler>> GetHttpHandlers()
    {
        var request = new RestRequest($"{SharpC2.API.Routes.V1.Handlers}/http");
        var response = await _client.ExecuteAsync<IEnumerable<HttpHandlerResponse>>(request);

        return response.Data.Select(h => (HttpHandler)h);
    }

    public async Task<HttpHandler> GetHttpHandler(string name)
    {
        var request = new RestRequest($"{SharpC2.API.Routes.V1.Handlers}/http/{name}");
        var response = await _client.ExecuteAsync<HttpHandlerResponse>(request);

        return response.IsSuccessStatusCode ? response.Data : null;
    }

    public async Task CreateHttpHandler(HttpHandlerRequest handlerRequest)
    {
        var request = new RestRequest($"{SharpC2.API.Routes.V1.Handlers}/http", Method.Post);
        request.AddParameter("application/json", JsonSerializer.Serialize(handlerRequest), ParameterType.RequestBody);

        await _client.ExecuteAsync(request);
    }

    public async Task<IEnumerable<SmbHandler>> GetSmbHandlers()
    {
        var request = new RestRequest($"{SharpC2.API.Routes.V1.Handlers}/smb");
        var response = await _client.ExecuteAsync<IEnumerable<SmbHandlerResponse>>(request);

        return response.Data.Select(h => (SmbHandler)h);
    }

    public async Task<SmbHandler> GetSmbHandler(string name)
    {
        var request = new RestRequest($"{SharpC2.API.Routes.V1.Handlers}/smb/{name}");
        var response = await _client.ExecuteAsync<SmbHandlerResponse>(request);

        return response.IsSuccessStatusCode ? response.Data : null;
    }

    public async Task CreateSmbHandler(SmbHandlerRequest handlerRequest)
    {
        var request = new RestRequest($"{SharpC2.API.Routes.V1.Handlers}/smb", Method.Post);
        request.AddParameter("application/json", JsonSerializer.Serialize(handlerRequest), ParameterType.RequestBody);

        await _client.ExecuteAsync(request);
    }
    
    public async Task<IEnumerable<TcpHandler>> GetTcpHandlers()
    {
        var request = new RestRequest($"{SharpC2.API.Routes.V1.Handlers}/tcp");
        var response = await _client.ExecuteAsync<IEnumerable<TcpHandlerResponse>>(request);

        return response.Data.Select(h => (TcpHandler)h);
    }
    
    public async Task<TcpHandler> GetTcpHandler(string name)
    {
        var request = new RestRequest($"{SharpC2.API.Routes.V1.Handlers}/tcp/{name}");
        var response = await _client.ExecuteAsync<TcpHandlerResponse>(request);

        return response.IsSuccessStatusCode ? response.Data : null;
    }
    
    public async Task CreateTcpHandler(TcpHandlerRequest handlerRequest)
    {
        var request = new RestRequest($"{SharpC2.API.Routes.V1.Handlers}/tcp", Method.Post);
        request.AddParameter("application/json", JsonSerializer.Serialize(handlerRequest), ParameterType.RequestBody);

        await _client.ExecuteAsync(request);
    }
    
    public async Task<IEnumerable<ExtHandler>> GetExtHandlers()
    {
        var request = new RestRequest($"{SharpC2.API.Routes.V1.Handlers}/ext");
        var response = await _client.ExecuteAsync<IEnumerable<ExtHandlerResponse>>(request);

        return response.Data.Select(h => (ExtHandler)h);
    }
    
    public async Task<ExtHandler> GetExtHandler(string name)
    {
        var request = new RestRequest($"{SharpC2.API.Routes.V1.Handlers}/ext/{name}");
        var response = await _client.ExecuteAsync<ExtHandlerResponse>(request);

        return response.IsSuccessStatusCode ? response.Data : null;
    }
    
    public async Task CreateExtHandler(ExtHandlerRequest handlerRequest)
    {
        var request = new RestRequest($"{SharpC2.API.Routes.V1.Handlers}/ext", Method.Post);
        request.AddParameter("application/json", JsonSerializer.Serialize(handlerRequest), ParameterType.RequestBody);

        await _client.ExecuteAsync(request);
    }

    public async Task HostFile(HostedFileRequest fileRequest)
    {
        var request = new RestRequest($"{SharpC2.API.Routes.V1.HostedFiles}", Method.Post);
        request.AddParameter("application/json", JsonSerializer.Serialize(fileRequest), ParameterType.RequestBody);

        await _client.ExecuteAsync(request);
    }

    public async Task<IEnumerable<HostedFile>> GetHostedFiles()
    {
        var request = new RestRequest($"{SharpC2.API.Routes.V1.HostedFiles}");
        var response = await _client.ExecuteAsync<IEnumerable<HostedFileResponse>>(request);

        return response.Data.Select(f => (HostedFile)f);
    }

    public async Task<HostedFile> GetHostedFile(string id)
    {
        var request = new RestRequest($"{SharpC2.API.Routes.V1.HostedFiles}/{id}");
        var response = await _client.ExecuteAsync<HostedFileResponse>(request);

        return response.IsSuccessStatusCode ? response.Data : null;
    }

    public async Task DeleteHostedFile(string id)
    {
        var request = new RestRequest($"{SharpC2.API.Routes.V1.HostedFiles}/{id}", Method.Delete);
        await _client.ExecuteAsync(request);
    }

    public async Task DeleteHandler(Handler handler)
    {
        var request = new RestRequest($"{SharpC2.API.Routes.V1.Handlers}/{handler.Name}", Method.Delete);
        await _client.ExecuteAsync(request);
    }

    public async Task<IEnumerable<Drone>> GetDrones()
    {
        var request = new RestRequest($"{SharpC2.API.Routes.V1.Drones}");
        var response = await _client.ExecuteAsync<IEnumerable<DroneResponse>>(request);

        return response.Data.Select(d => (Drone)d);
    }
    
    public async Task<Drone> GetDrone(string id)
    {
        var request = new RestRequest($"{SharpC2.API.Routes.V1.Drones}/{id}");
        var response = await _client.ExecuteAsync<DroneResponse>(request);

        return response.IsSuccessStatusCode ? response.Data : null;
    }

    public async Task DeleteDrone(string id)
    {
        var request = new RestRequest($"{SharpC2.API.Routes.V1.Drones}/{id}", Method.Delete);
        await _client.ExecuteAsync(request);
    }

    public async Task TaskDrone(string droneId, TaskRequest taskRequest)
    {
        var request = new RestRequest($"{SharpC2.API.Routes.V1.Tasks}/{droneId}", Method.Post);
        request.AddParameter("application/json", JsonSerializer.Serialize(taskRequest), ParameterType.RequestBody);

        await _client.ExecuteAsync(request);
    }
    
    public async Task<IEnumerable<TaskRecord>> GetTasks(string droneId)
    {
        var request = new RestRequest($"{SharpC2.API.Routes.V1.Tasks}/{droneId}");
        var response = await _client.ExecuteAsync<IEnumerable<TaskRecordResponse>>(request);

        return response.Data.Select(t => (TaskRecord)t);
    }
    
    public async Task<TaskRecord> GetTask(string droneId, string taskId)
    {
        var request = new RestRequest($"{SharpC2.API.Routes.V1.Tasks}/{droneId}/{taskId}");
        var response = await _client.ExecuteAsync<TaskRecordResponse>(request);

        return response.IsSuccessStatusCode ? response.Data : null;
    }

    public async Task DeleteTask(string droneId, string taskId)
    {
        var request = new RestRequest($"{SharpC2.API.Routes.V1.Tasks}/{droneId}/{taskId}", Method.Delete);
        await _client.ExecuteAsync(request);
    }

    public async Task<MemoryStream> GeneratePayload(Handler handler, int format)
    {
        var request = new RestRequest($"{SharpC2.API.Routes.V1.Payloads}?handler={handler.Name}&format={format}");
        var response = await _client.DownloadDataAsync(request);
        
        return response is null
            ? new MemoryStream()
            : new MemoryStream(response);
    }

    public async Task<IEnumerable<WebLogEvent>> GetWebLogs()
    {
        var request = new RestRequest($"{SharpC2.API.Routes.V1.Events}/web");
        var response = await _client.ExecuteAsync<IEnumerable<WebLogEventResponse>>(request);

        return response.Data.Select(e => (WebLogEvent)e);
    }

    public async Task<WebLogEvent> GetWebLog(string id)
    {
        var request = new RestRequest($"{SharpC2.API.Routes.V1.Events}/web/{id}");
        var response = await _client.ExecuteAsync<WebLogEventResponse>(request);

        return response.IsSuccessStatusCode ? response.Data : null;
    }

    public async Task<IEnumerable<UserAuthEvent>> GetAuthEvents()
    {
        var request = new RestRequest($"{SharpC2.API.Routes.V1.Events}/auth");
        var response = await _client.ExecuteAsync<IEnumerable<UserAuthEventResponse>>(request);

        return response.Data.Select(e => (UserAuthEvent)e);
    }
    
    public async Task<UserAuthEvent> GetAuthEvent(string id)
    {
        var request = new RestRequest($"{SharpC2.API.Routes.V1.Events}/auth/{id}");
        var response = await _client.ExecuteAsync<UserAuthEventResponse>(request);

        return response.IsSuccessStatusCode ? response.Data : null;
    }

    public async Task<IEnumerable<ReversePortForward>> GetReversePortForwards()
    {
        var request = new RestRequest($"{SharpC2.API.Routes.V1.Pivots}/rportfwd");
        var response = await _client.ExecuteAsync<IEnumerable<ReversePortForwardResponse>>(request);

        return response.Data.Select(f => (ReversePortForward)f);
    }

    public async Task<ReversePortForward> GetReversePortForward(string id)
    {
        var request = new RestRequest($"{SharpC2.API.Routes.V1.Pivots}/rportfwd/{id}");
        var response = await _client.ExecuteAsync<ReversePortForwardResponse>(request);

        return response.IsSuccessStatusCode ? response.Data : null;
    }

    public async Task CreateReversePortForward(ReversePortForwardRequest fwdRequest)
    {
        var request = new RestRequest($"{SharpC2.API.Routes.V1.Pivots}/rportfwd", Method.Post);
        request.AddParameter("application/json", JsonSerializer.Serialize(fwdRequest), ParameterType.RequestBody);

        await _client.ExecuteAsync(request);
    }

    public async Task DeleteReversePortForward(string id)
    {
        var request = new RestRequest($"{SharpC2.API.Routes.V1.Pivots}/rportfwd/{id}", Method.Delete);
        await _client.ExecuteAsync(request);
    }

    public async Task<IEnumerable<SocksProxy>> GetSocksProxies()
    {
        var request = new RestRequest($"{SharpC2.API.Routes.V1.Pivots}/socks");
        var response = await _client.ExecuteAsync<IEnumerable<SocksResponse>>(request);

        return response.Data.Select(f => (SocksProxy)f);
    }

    public async Task<SocksProxy> GetSocksProxy(string id)
    {
        var request = new RestRequest($"{SharpC2.API.Routes.V1.Pivots}/socks/{id}");
        var response = await _client.ExecuteAsync<SocksResponse>(request);

        return response.IsSuccessStatusCode ? response.Data : null;
    }

    public async Task CreateSocksProxy(SocksRequest socksRequest)
    {
        var request = new RestRequest($"{SharpC2.API.Routes.V1.Pivots}/socks", Method.Post);
        request.AddParameter("application/json", JsonSerializer.Serialize(socksRequest), ParameterType.RequestBody);

        await _client.ExecuteAsync(request);
    }

    public async Task DeleteSocksProxy(string id)
    {
        var request = new RestRequest($"{SharpC2.API.Routes.V1.Pivots}/socks/{id}", Method.Delete);
        await _client.ExecuteAsync(request);
    }

    public async Task<IEnumerable<SharpC2Webhook>> GetWebhooks()
    {
        var request = new RestRequest(SharpC2.API.Routes.V1.Webhooks);
        var response = await _client.ExecuteAsync<IEnumerable<WebhookResponse>>(request);

        return response.Data.Select(h => (SharpC2Webhook)h);
    }
    
    public async Task<SharpC2Webhook> GetWebhook(string name)
    {
        var request = new RestRequest($"{SharpC2.API.Routes.V1.Webhooks}/{name}");
        var response = await _client.ExecuteAsync<WebhookResponse>(request);

        return response.Data;
    }

    public async Task DeleteWebhook(string name)
    {
        var request = new RestRequest($"{SharpC2.API.Routes.V1.Webhooks}/{name}", Method.Delete);
        await _client.ExecuteAsync<WebhookResponse>(request);
    }

    public async Task CreateWebhook(WebhookRequest webhookRequest)
    {
        var request = new RestRequest(SharpC2.API.Routes.V1.Webhooks, Method.Post);
        request.AddParameter("application/json", JsonSerializer.Serialize(webhookRequest), ParameterType.RequestBody);

        await _client.ExecuteAsync(request);
    }
}