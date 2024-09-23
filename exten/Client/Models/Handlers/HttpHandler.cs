using SharpC2.API.Responses;

namespace Client.Models.Handlers;

public sealed class HttpHandler : Handler
{
    public int BindPort { get; set; }
    public string ConnectAddress { get; set; }
    public int ConnectPort { get; set; }

    public static implicit operator HttpHandler(HttpHandlerResponse response)
    {
        if (response is null)
            return null;

        return new HttpHandler
        {
            Id = response.Id,
            Name = response.Name,
            Profile = response.Profile,
            BindPort = response.BindPort,
            ConnectAddress = response.ConnectAddress,
            ConnectPort = response.ConnectPort,
            PayloadType = (PayloadType)response.PayloadType
        };
    }
}