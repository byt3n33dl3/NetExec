using System.Net;
using System.Security.Cryptography.X509Certificates;

using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.FileProviders;

using SharpC2.API.Requests;
using SharpC2.API.Responses;

using TeamServer.Hubs;
using TeamServer.Interfaces;
using TeamServer.Middleware;
using TeamServer.Utilities;

namespace TeamServer.Handlers;

public class HttpHandler : Handler
{
    private CancellationTokenSource _tokenSource;

    public int BindPort { get; set; }
    public string ConnectAddress { get; set; }
    public int ConnectPort { get; set; }
    public bool Secure { get; set; }
    public byte[] PfxCertificate { get; set; }
    public string PfxPassword { get; set; }

    public override HandlerType HandlerType
        => HandlerType.HTTP;

    public string DataDirectory { get; set; }

    private void CreateDataDirectory()
    {
        DataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Data", Id);

        // create data directory
        if (!Directory.Exists(DataDirectory))
            Directory.CreateDirectory(DataDirectory);
    }

    public Task Start()
    {
        // ensure data directory exists
        CreateDataDirectory();

        _tokenSource = new CancellationTokenSource();

        var host = new HostBuilder()
            .ConfigureWebHostDefaults(ConfigureWebHost)
            .Build();

        return host.RunAsync(_tokenSource.Token);
    }

    private void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("name", Name);
        builder.UseSetting("profile", C2Profile.Name);
        builder.UseUrls(Secure ? $"https://0.0.0.0:{BindPort}" : $"http://0.0.0.0:{BindPort}");
        builder.Configure(ConfigureApp);
        builder.ConfigureServices(ConfigureServices);
        builder.ConfigureKestrel(ConfigureKestrel);
    }

    private void ConfigureApp(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseWebLogMiddleware();
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(DataDirectory),
            ServeUnknownFileTypes = true,
            DefaultContentType = "test/plain"
        });
        app.UseEndpoints(ConfigureEndpoints);
    }

    private void ConfigureEndpoints(IEndpointRouteBuilder endpoint)
    {
        var paths = C2Profile.Http.GetPaths.Concat(C2Profile.Http.PostPaths);

        foreach (var path in paths)
        {
            endpoint.MapControllerRoute(path, path, new
            {
                controller = "HttpHandler", action = "RouteDrone"
            });
        }
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();

        services.AddSingleton(Program.GetService<IProfileService>());
        services.AddSingleton(Program.GetService<IDatabaseService>());
        services.AddSingleton(Program.GetService<ICryptoService>());
        services.AddSingleton(Program.GetService<IDroneService>());
        services.AddSingleton(Program.GetService<IPeerToPeerService>());
        services.AddSingleton(Program.GetService<ITaskService>());
        services.AddSingleton(Program.GetService<IServerService>());
        services.AddSingleton(Program.GetService<IEventService>());
        services.AddSingleton(Program.GetService<IHubContext<NotificationHub, INotificationHub>>());
    }

    private void ConfigureKestrel(KestrelServerOptions kestrel)
    {
        kestrel.AddServerHeader = false;
        kestrel.Listen(IPAddress.Any, BindPort, ListenOptions);
    }

    private void ListenOptions(ListenOptions o)
    {
        o.Protocols = HttpProtocols.Http1AndHttp2;

        if (!Secure)
            return;

        var cert = new X509Certificate2(PfxCertificate, PfxPassword);
        o.UseHttps(cert);
    }

    public void Stop()
    {
        if (_tokenSource is null || _tokenSource.IsCancellationRequested)
            return;

        _tokenSource.Cancel();
        _tokenSource.Dispose();
        
        // delete hosted files
        Directory.Delete(DataDirectory, true);
    }
    
    public static implicit operator HttpHandler(HttpHandlerRequest request)
    {
        return new HttpHandler
        {
            Id = Helpers.GenerateShortGuid(),
            Name = request.Name,
            BindPort = request.BindPort,
            ConnectAddress = request.ConnectAddress,
            ConnectPort = request.ConnectPort,
            Secure = request.Secure,
            PfxCertificate = request.PfxCertificate,
            PfxPassword = request.PfxPassword,
            PayloadType = request.Secure ? PayloadType.HTTPS : PayloadType.HTTP
        };
    }

    public static implicit operator HttpHandlerResponse(HttpHandler handler)
    {
        return new HttpHandlerResponse
        {
            Id = handler.Id,
            Name = handler.Name,
            Profile = handler.C2Profile.Name,
            BindPort = handler.BindPort,
            ConnectAddress = handler.ConnectAddress,
            ConnectPort = handler.ConnectPort,
            Secure = handler.Secure,
            HandlerType = (int)handler.HandlerType,
            PayloadType = (int)handler.PayloadType,
        };
    }
}