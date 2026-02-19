using System.Collections.Generic;
using Newtonsoft.Json;

namespace AlianceGuard.Discord;

public class DiscordEmbed
{
    [JsonProperty("title")]
    public string Title { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("color")]
    public int Color { get; set; }

    [JsonProperty("fields")]
    public List<DiscordEmbedField> Fields { get; set; } = new List<DiscordEmbedField>();

    [JsonProperty("footer")]
    public DiscordEmbedFooter Footer { get; set; }

    [JsonProperty("timestamp")]
    public string Timestamp { get; set; }

    [JsonProperty("thumbnail")]
    public DiscordEmbedThumbnail Thumbnail { get; set; }
}