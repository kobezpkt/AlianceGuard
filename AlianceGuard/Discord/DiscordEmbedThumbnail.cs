using Newtonsoft.Json;

namespace AlianceGuard.Discord;

public class DiscordEmbedThumbnail
{
    [JsonProperty("url")]
    public string Url { get; set; }

}   