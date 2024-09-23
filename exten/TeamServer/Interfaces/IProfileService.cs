using TeamServer.C2Profiles;

namespace TeamServer.Interfaces;

public interface IProfileService
{
    Task<C2Profile> Get(string name);
    Task<IEnumerable<C2Profile>> Get();
}