using TeamServer.C2Profiles;
using TeamServer.Interfaces;

namespace TeamServer.Services;

public sealed class ProfileService : IProfileService
{
    public async Task<C2Profile> Get(string name)
    {
        var profiles = await Get();
        return profiles.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IEnumerable<C2Profile>> Get()
    {
        return await ReadProfiles();
    }

    private static async Task<IEnumerable<C2Profile>> ReadProfiles()
    {
        var profiles = new List<C2Profile>();
        
        var path = Path.Combine(Directory.GetCurrentDirectory(), "C2Profiles");
        var files = Directory.GetFiles(path, "*.yaml");
        var serializer = new YamlDotNet.Serialization.Deserializer();

        foreach (var file in files)
        {
            var yaml = await File.ReadAllTextAsync(file);

            try
            {
                var profile = serializer.Deserialize<C2Profile>(yaml);
                profiles.Add(profile);
            }
            catch
            {
                // ignore
            }
        }

        return profiles;
    }
}