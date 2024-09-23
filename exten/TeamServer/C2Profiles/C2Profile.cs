using SharpC2.API.Responses;

namespace TeamServer.C2Profiles;

public sealed class C2Profile
{
    public string Name { get; set; } = "default";
    public HttpProfile Http { get; set; } = new();

    public sealed class HttpProfile
    {
        public int Sleep { get; set; } = 30;
        public int Jitter { get; set; }
        public string[] GetPaths { get; set; } = { "/index.php" };
        public string[] PostPaths { get; set; } = { "/submit.php" };
    }

    public static implicit operator C2ProfileResponse(C2Profile profile)
    {
        return profile is null
            ? null
            : new C2ProfileResponse
            {
                Name = profile.Name,
                Http = new C2ProfileResponse.HttpProfileResponse
                {
                    Sleep = profile.Http.Sleep,
                    Jitter = profile.Http.Jitter,
                    GetPaths = profile.Http.GetPaths,
                    PostPaths = profile.Http.PostPaths
                }
            };
    }
}