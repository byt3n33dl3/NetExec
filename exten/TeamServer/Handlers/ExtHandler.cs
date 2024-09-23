using System.Net;
using System.Net.Sockets;

using SharpC2.API.Requests;
using SharpC2.API.Responses;

using TeamServer.Drones;
using TeamServer.Interfaces;
using TeamServer.Messages;
using TeamServer.Utilities;

namespace TeamServer.Handlers;

public class ExtHandler : Handler
{
    public override HandlerType HandlerType
        => HandlerType.EXTERNAL;

    public int BindPort { get; set; }

    private readonly IPayloadService _payloads;
    private readonly ICryptoService _crypto;
    private readonly IServerService _server;
    
    private readonly CancellationTokenSource _tokenSource = new();
    private readonly ManualResetEvent _signal = new(false);

    public ExtHandler()
    {
        _payloads = Program.GetService<IPayloadService>();
        _crypto = Program.GetService<ICryptoService>();
        _server = Program.GetService<IServerService>();
    }

    public Task Start()
    {
        return Task.Run(ListenerThread);
    }

    private void ListenerThread()
    {
        var listener = new TcpListener(new IPEndPoint(IPAddress.Any, BindPort));
        listener.Start(100);

        while (!_tokenSource.IsCancellationRequested)
        {
            _signal.Reset();
            listener.BeginAcceptTcpClient(ConnectCallback, listener);
            _signal.WaitOne();
        }
        
        // stop listener
        listener.Stop();
        
        // dispose token
        _tokenSource.Dispose();
    }

    private void ConnectCallback(IAsyncResult ar)
    {
        _signal.Set();
        
        if (ar.AsyncState is not TcpListener listener)
            return;

        var controller = listener.EndAcceptTcpClient(ar);
        
        // run in own thread
        var thread = new Thread(HandleClient);
        thread.Start(controller);
    }

    private async void HandleClient(object obj)
    {
        if (obj is not TcpClient controller)
            return;

        // get payload
        var payload = await _payloads.GeneratePayload(this, PayloadFormat.ASSEMBLY);
        
        // send it to the controller
        await controller.WriteClient(payload);
        
        Metadata metadata = null;
        
        // drop into a loop
        while (controller.Connected)
        {
            if (controller.DataAvailable())
            {
                // read from controller
                var inbound = await controller.ReadClient();
                
                // deserialize it
                var inFrame = inbound.Deserialize<C2Frame>();
                
                // if this is a check-in frame, read the metadata
                if (inFrame.Type == FrameType.CHECK_IN)
                    metadata = await _crypto.Decrypt<Metadata>(inFrame.Data);

                // hand it off
                await _server.HandleInboundFrame(inFrame);
            }
            
            // get outbound frames
            if (metadata is not null)
            {
                var outFrames = (await _server.GetOutboundFrames(metadata)).ToArray();

                // p2p drones only take 1 frame at a time
                foreach (var outFrame in outFrames)
                    await controller.WriteClient(outFrame.Serialize());
            }

            await Task.Delay(100);
        }
    }

    public void Stop()
    {
        _tokenSource.Cancel();
    }

    public static implicit operator ExtHandler(ExtHandlerRequest request)
    {
        return new ExtHandler
        {
            Id = Helpers.GenerateShortGuid(),
            Name = request.Name,
            BindPort = request.BindPort,
            PayloadType = PayloadType.EXTERNAL
        };
    }

    public static implicit operator ExtHandlerResponse(ExtHandler handler)
    {
        return new ExtHandlerResponse
        {
            Id = handler.Id,
            Name = handler.Name,
            BindPort = handler.BindPort
        };
    }
}