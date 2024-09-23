using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

using TeamServer.Events;

namespace TeamServer.Webhooks;

public sealed class SlackWebhook : SharpC2Webhook
{
    public override WebhookConsumer Consumer
        => WebhookConsumer.SLACK;

    public override async Task Send(SharpC2Event ev)
    {
        if (ev.Type != EventType)
            return;
        
        var message = ev switch
        {
            UserAuthEvent userAuthEvent => new SlackMessage
            {
                Blocks = new List<Block>
                {
                    new()
                    {
                        Type = "header",
                        BlockText = new BlockText
                        {
                            Type = "plain_text", Text = "User Authentication", Emoji = true
                        }
                    },
                    new()
                    {
                        Type = "section",
                        Fields = new List<Field>
                        {
                            new() { Type = "mrkdwn", Text = $"*Nick:*\n{userAuthEvent.Nick}" },
                            new()
                            {
                                Type = "mrkdwn",
                                Text =
                                    $"*Result:*\n{(userAuthEvent.Result ? "Success" : "Failure")}"
                            }
                        }
                    },
                    new()
                    {
                        Type = "section",
                        Fields = new List<Field>
                        {
                            new() { Type = "mrkdwn", Text = $"*When:*\n{userAuthEvent.Date:u}" }
                        }
                    }
                }
            },
            WebLogEvent webLogEvent => new SlackMessage
            {
                Blocks = new List<Block>
                {
                    new()
                    {
                        Type = "header",
                        BlockText = new BlockText { Type = "plain_text", Text = "Web Log", Emoji = true }
                    },
                    new()
                    {
                        Type = "section",
                        Fields = new List<Field>
                        {
                            new() { Type = "mrkdwn", Text = $"*Uri:*\n{webLogEvent.Uri}" },
                            new() { Type = "mrkdwn", Text = $"*Method:*\n{webLogEvent.Method}" }
                        }
                    },
                    new()
                    {
                        Type = "section",
                        Fields = new List<Field>
                        {
                            new()
                            {
                                Type = "mrkdwn", Text = $"*Source IP:*\n{webLogEvent.SourceAddress}"
                            },
                            new() { Type = "mrkdwn", Text = $"*User Agent:*\n{webLogEvent.UserAgent}" }
                        }
                    },
                    new()
                    {
                        Type = "section",
                        Fields = new List<Field>
                        {
                            new() { Type = "mrkdwn", Text = $"*When:*\n{webLogEvent.Date:u}" }
                        }
                    }
                }
            },
            _ => null
        };

        if (message is null)
            return;

        var opts = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
        var json = JsonContent.Create(message, new MediaTypeHeaderValue("application/json"), opts);
        
        using var client = new HttpClient();
        await client.PostAsync(Url, json);
    }

    private class SlackMessage
    {
        [JsonPropertyName("blocks")]
        public IEnumerable<Block> Blocks { get; set; }
    }

    private class Block
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }
        
        [JsonPropertyName("text")]
        public BlockText BlockText { get; set; }
        
        [JsonPropertyName("fields")]
        public IEnumerable<Field> Fields { get; set; }
    }

    private class BlockText
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }
        
        [JsonPropertyName("text")]
        public string Text { get; set; }
        
        [JsonPropertyName("emoji")]
        public bool Emoji { get; set; }
    }

    private class Field
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }  = "";
        
        [JsonPropertyName("text")]
        public string Text { get; set; }  = "";
    }
}