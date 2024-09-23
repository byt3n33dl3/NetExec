using System.Net;
using System.Security.Cryptography.X509Certificates;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TeamServer.Events;
using TeamServer.Filters;
using TeamServer.Hubs;
using TeamServer.Interfaces;
using TeamServer.Services;
using TeamServer.Utilities;
using TeamServer.Webhooks;

namespace TeamServer;

internal static class Program
{
    private const string ServerPfx = "server.pfx";

    private static WebApplication _app;
    
    internal static T GetService<T>()
        => _app.Services.GetRequiredService<T>();
    
    public static async Task Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: ./TeamServer <ip> <password>");
            return;
        }
        
        if (!IPAddress.TryParse(args[0], out var address))
        {
            Console.WriteLine("Invalid IP address.");
            return;
        }

        var pfxPath = Path.Combine(Directory.GetCurrentDirectory(), ServerPfx);
        X509Certificate2 pfx;
        
        // check if pfx exists
        if (!File.Exists(pfxPath))
        {
            // generate self-signed cert
            pfx = Helpers.GenerateSelfSignedCertificate(address.ToString());
            await File.WriteAllBytesAsync(pfxPath, pfx.Export(X509ContentType.Pkcs12));
        }
        else
        {
            pfx = new X509Certificate2(await File.ReadAllBytesAsync(ServerPfx));
        }
        
        // print thumbprint
        Console.WriteLine($"Certificate thumbprint: {pfx.Thumbprint}");
        
        var password = args[1];

        // initialize auth service
        var authService = new AuthenticationService();
        
        var jwtKey = Helpers.GeneratePasswordHash(password, out _);
        authService.SetServerPassword(password, jwtKey);
        
        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.ConfigureKestrel(ConfigureKestrel);
        
        // configure jwt auth
        builder.Services
            .AddAuthentication(a =>
            {
                a.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                a.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(j =>
            {
                j.SaveToken = true;
                j.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(jwtKey)
                };
            });
        
        // ensure swagger can auth as well
        builder.Services.AddSwaggerGen(s =>
        {
            s.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                In = ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme.\r\n\r\n"
            });
            s.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });
        
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddControllers(ConfigureControllers);
        builder.Services.AddSignalR();
        
        builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
        builder.Services.AddSingleton<IAuthenticationService>(authService);
        builder.Services.AddSingleton<IHandlerService, HandlerService>();
        builder.Services.AddSingleton<ITaskService, TaskService>();
        builder.Services.AddSingleton<IServerService, ServerService>();
        builder.Services.AddSingleton<IPeerToPeerService, PeerToPeerService>();
        builder.Services.AddSingleton<ISocksService, SocksService>();
        builder.Services.AddSingleton<IEventService, EventService>();
        builder.Services.AddSingleton<IWebhookService, WebhookService>();

        builder.Services.AddTransient<IProfileService, ProfileService>();
        builder.Services.AddTransient<ICryptoService, CryptoService>();
        builder.Services.AddTransient<IDroneService, DroneService>();
        builder.Services.AddTransient<IPayloadService, PayloadService>();
        builder.Services.AddTransient<IHostedFilesService, HostedFileService>();
        builder.Services.AddTransient<IReversePortForwardService, ReversePortForwardService>();

        _app = builder.Build();
        
        // build p2p graph from known drones
        var droneService = GetService<IDroneService>();
        var drones = await droneService.Get();
        
        var p2pService = GetService<IPeerToPeerService>();
        p2pService.InitFromDrones(drones);
        
        // load socks proxies
        var socks = GetService<ISocksService>();
        await socks.LoadFromDatabase();

        // load saved handlers from DB
        await LoadHandlersFromDatabase();
        
        // get saved webhooks and register new events
        await LoadWebhooks();
        
        if (_app.Environment.IsDevelopment())
        {
            _app.UseSwagger();
            _app.UseSwaggerUI();
        }

        _app.UseHttpsRedirection();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.MapControllers();
        
        _app.MapHub<NotificationHub>("/SharpC2");
        
        await _app.RunAsync();
    }
    
    private static void ConfigureKestrel(KestrelServerOptions kestrel)
    {
        kestrel.Listen(IPAddress.Any, 50050, ListenOptions);
    }
    
    private static void ListenOptions(ListenOptions o)
    {
        o.Protocols = HttpProtocols.Http1AndHttp2;
        o.UseHttps(ServerPfx);
    }
    
    private static void ConfigureControllers(MvcOptions opts)
    {
        opts.Filters.Add<InjectionFilters>();
    }
    
    private static async Task LoadHandlersFromDatabase()
    {
        var service = (HandlerService) GetService<IHandlerService>();
        await service.LoadHandlersFromDb();
    }

    private static async Task LoadWebhooks()
    {
        var webhookService = GetService<IWebhookService>();
        await ((WebhookService)webhookService).LoadWebhooksFromDb();
        
        var eventService = GetService<IEventService>();

        var webhooks = webhookService.Get<SharpC2Webhook>();
        
        foreach (var webhook in webhooks)
        {
            Task Callback(SharpC2Event ev) => webhook.Send(ev);
            eventService.SubscribeEvent(webhook.Id, Callback);
        }
    }
}