using Newtonsoft.Json;

namespace AlianceGuard.Discord;

public class DiscordEmbedField
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("value")]
    public string Value { get; set; }

    [JsonProperty("inline")]
    public bool Inline { get; set; }
}