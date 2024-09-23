namespace SharpC2.API.Responses;

public sealed class WebhookResponse
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int Consumer { get; set; }
    public int Event { get; set; }
    public string Url { get; set; }
}