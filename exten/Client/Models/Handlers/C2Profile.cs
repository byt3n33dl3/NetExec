using SharpC2.API.Responses;

namespace Client.Models.Handlers;

public sealed class C2Profile
{
    public string Name { get; set; }

    public override string ToString() 
        => Name;

    public HttpProfile Http { get; set; }
    
    public sealed class HttpProfile
    {
        public int Sleep { get; set; }
        public int Jitter { get; set; }
        public string[] GetPaths { get; set; }
        public string[] PostPaths { get; set; }
    }

    public static implicit operator C2Profile(C2ProfileResponse response)
    {
        return response is null
            ? null
            : new C2Profile
            {
                Name = response.Name,
                Http = new HttpProfile
                {
                    Sleep = response.Http.Sleep,
                    Jitter = response.Http.Jitter,
                    GetPaths = response.Http.GetPaths,
                    PostPaths = response.Http.PostPaths
                }
            };
    }
}