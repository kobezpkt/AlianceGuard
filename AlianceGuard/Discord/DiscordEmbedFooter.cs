using Newtonsoft.Json;

namespace AlianceGuard.Discord
{
    public class DiscordEmbedFooter
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("icon_url")]
        public string IconUrl { get; set; }
    }
}