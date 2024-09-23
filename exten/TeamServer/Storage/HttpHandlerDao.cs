using SQLite;

using TeamServer.Handlers;

namespace TeamServer.Storage;

[Table("http_handlers")]
public sealed class HttpHandlerDao
{
    [PrimaryKey, Column("id")]
    public string Id { get; set; }
    
    [Column("name")]
    public string Name { get; set; }
    
    [Column("profile")]
    public string C2Profile { get; set; }

    [Column("bind_port")]
    public int BindPort { get; set; }
    
    [Column("connect_address")]
    public string ConnectAddress { get; set; }
    
    [Column("connect_port")]
    public int ConnectPort { get; set; }
    
    [Column("secure")]
    public bool Secure { get; set; }
    
    [Column("payload_type")]
    public int PayloadType { get; set; }

    [Column("pfx_cert")]
    public byte[] PfxCertificate { get; set; }

    [Column("pfx_pass")]
    public string PfxPassword { get; set; }

    public static implicit operator HttpHandlerDao(HttpHandler handler)
    {
        return new HttpHandlerDao
        {
            Id = handler.Id,
            Name = handler.Name,
            C2Profile = handler.C2Profile.Name,
            BindPort = handler.BindPort,
            ConnectAddress = handler.ConnectAddress,
            ConnectPort = handler.ConnectPort,
            Secure = handler.Secure,
            PayloadType = (int)handler.PayloadType,
            PfxCertificate = handler.PfxCertificate,
            PfxPassword = handler.PfxPassword
        };
    }

    public static implicit operator HttpHandler(HttpHandlerDao dao)
    {
        return new HttpHandler
        {
            Id = dao.Id,
            Name = dao.Name,
            BindPort = dao.BindPort,
            ConnectAddress = dao.ConnectAddress,
            ConnectPort = dao.ConnectPort,
            Secure = dao.Secure,
            PayloadType = (PayloadType)dao.PayloadType,
            PfxCertificate = dao.PfxCertificate,
            PfxPassword = dao.PfxPassword
        };
    }
}