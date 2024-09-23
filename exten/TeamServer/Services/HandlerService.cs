using TeamServer.C2Profiles;
using TeamServer.Handlers;
using TeamServer.Interfaces;
using TeamServer.Storage;

namespace TeamServer.Services;

public class HandlerService : IHandlerService
{
    private readonly IDatabaseService _db;
    private readonly IProfileService _profiles;

    private readonly List<Handler> _handlers = new();

    public HandlerService(IDatabaseService db, IProfileService profiles)
    {
        _db = db;
        _profiles = profiles;
    }

    public async Task LoadHandlersFromDb()
    {
        var conn = _db.GetAsyncConnection();
        
        var http = await conn.Table<HttpHandlerDao>().ToArrayAsync();
        var tcp = await conn.Table<TcpHandlerDao>().ToArrayAsync();
        var smb = await conn.Table<SmbHandlerDao>().ToArrayAsync();
        var ext = await conn.Table<ExtHandlerDao>().ToArrayAsync();
        
        foreach (var dao in http)
        {
            var handler = (HttpHandler)dao;
            
            // recover the profile
            var profile = await _profiles.Get(dao.C2Profile);
            handler.C2Profile = profile ?? new C2Profile();
            
            _ = handler.Start();
            _handlers.Add(handler);
        }

        foreach (var dao in tcp)
            _handlers.Add((TcpHandler)dao);

        foreach (var dao in smb)
            _handlers.Add((SmbHandler)dao);

        foreach (var dao in ext)
        {
            var handler = (ExtHandler)dao;
            _ = handler.Start();
            _handlers.Add(handler);
        }
    }

    public async Task Add(Handler handler)
    {
        // keep running handlers in memory
        _handlers.Add(handler);
        
        // and write to the db
        var conn = _db.GetAsyncConnection();

        switch (handler.HandlerType)
        {
            case HandlerType.HTTP:
            {
                await conn.InsertAsync((HttpHandlerDao)handler);
                break;
            }

            case HandlerType.SMB:
            {
                await conn.InsertAsync((SmbHandlerDao)handler);
                break;
            }

            case HandlerType.TCP:
            {
                await conn.InsertAsync((TcpHandlerDao)handler);
                break;
            }

            case HandlerType.EXTERNAL:
            {
                await conn.InsertAsync((ExtHandlerDao)handler);
                break;
            }
            
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public T Get<T>(string name) where T : Handler
    {
        return (T)_handlers.FirstOrDefault(h => h.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<T> Get<T>() where T : Handler
    {
        return _handlers
            .Where(h => h.GetType() == typeof(T))
            .Cast<T>();
    }

    public async Task Update(Handler handler)
    {
        var conn = _db.GetAsyncConnection();

        switch (handler.HandlerType)
        {
            case HandlerType.HTTP:
            {
                await conn.UpdateAsync((HttpHandlerDao)handler);
                break;
            }

            case HandlerType.SMB:
            {
                await conn.UpdateAsync((SmbHandlerDao)handler);
                break;
            }

            case HandlerType.TCP:
            {
                await conn.UpdateAsync((TcpHandlerDao)handler);
                break;
            }
            
            case HandlerType.EXTERNAL:
                await conn.UpdateAsync((ExtHandlerDao)handler);
                break;
            
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public async Task Delete(Handler handler)
    {
        // remove from memory
        _handlers.Remove(handler);
        
        // remove from db
        var conn = _db.GetAsyncConnection();

        switch (handler.HandlerType)
        {
            case HandlerType.HTTP:
            {
                await conn.DeleteAsync((HttpHandlerDao)handler);
                break;
            }

            case HandlerType.SMB:
            {
                await conn.DeleteAsync((SmbHandlerDao)handler);
                break;
            }

            case HandlerType.TCP:
            {
                await conn.DeleteAsync((TcpHandlerDao)handler);
                break;
            }

            case HandlerType.EXTERNAL:
            {
                await conn.DeleteAsync((ExtHandlerDao)handler);
                break;
            }
            
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}