using SQLite;

using TeamServer.Handlers;

namespace TeamServer.Storage;

[Table("smb_handlers")]
public sealed class SmbHandlerDao
{
    [PrimaryKey, Column("id")]
    public string Id { get; set; }
    
    [Column("name")]
    public string Name { get; set; }

    [Column("pipe_name")]
    public string PipeName { get; set; }
    
    [Column("payload_type")]
    public int PayloadType { get; set; }

    public static implicit operator SmbHandlerDao(SmbHandler handler)
    {
        return new SmbHandlerDao
        {
            Id = handler.Id,
            Name = handler.Name,
            PipeName = handler.PipeName,
            PayloadType = (int)handler.PayloadType
        };
    }
    
    public static implicit operator SmbHandler(SmbHandlerDao dao)
    {
        return new SmbHandler
        {
            Id = dao.Id,
            Name = dao.Name,
            PipeName = dao.PipeName,
            PayloadType = (PayloadType)dao.PayloadType
        };
    }
}