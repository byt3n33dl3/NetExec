using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

using ExternalC2.NET.Server;

namespace DemoController;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("demo-controller.exe <address> <port>");
            return;
        }

        var target = IPAddress.Parse(args[0]);
        var port = int.Parse(args[1]);

        // wait for a connection from a client
        var listener = new TcpListener(IPAddress.Loopback, 9999);
        listener.Start(100);

        while (true)
        {
            // block until a client connects
            var client = await listener.AcceptTcpClientAsync();
            
            // handle each client in its own task
            var controller = new ServerController(target, port);
            _ = Task.Run(async () => await HandleClient(controller, client));
        }
    }

    private static async Task HandleClient(ServerController controller, TcpClient client)
    {
        controller.OnDataFromTeamServer += async delegate(byte[] data)
        {
            // send any data received from the team server to the client
            await client.WriteData(data);
        };

        // run the controller
        _ = controller.Start();
        
        // read from the client
        while (client.Connected)
        {
            // this is upstream from the drone
            if (client.DataAvailable())
            {
                var upstream = await client.ReadData();
                
                // give it to the controller
                await controller.SendData(upstream);
            }

            await Task.Delay(100);
        }
        
        controller.Dispose();
    }
}