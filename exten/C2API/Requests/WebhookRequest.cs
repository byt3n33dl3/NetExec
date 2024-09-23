namespace SharpC2.API.Requests;

public sealed class WebhookRequest
{
    public string Name { get; set; }
    public int Consumer { get; set; }
    public int Event { get; set; }
    public string Url { get; set; }
}