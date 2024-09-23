namespace SharpC2.API.Responses;

public sealed class C2ProfileResponse
{
    public string Name { get; set; }
    public HttpProfileResponse Http { get; set; }

    public sealed class HttpProfileResponse
    {
        public int Sleep { get; set; }
        public int Jitter { get; set; }
        public string[] GetPaths { get; set; }
        public string[] PostPaths { get; set; }
    }
}