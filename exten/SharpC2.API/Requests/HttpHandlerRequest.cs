namespace SharpC2.API.Requests;

public class HttpHandlerRequest
{
    public string Name { get; set; }
    public string Profile { get; set; }
    public int BindPort { get; set; }
    public string ConnectAddress { get; set; }
    public int ConnectPort { get; set; }
    public bool Secure { get; set; }
    public byte[] PfxCertificate { get; set; }
    public string PfxPassword { get; set; }
}