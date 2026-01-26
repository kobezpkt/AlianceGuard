using Newtonsoft.Json;
using System.Collections.Generic;

namespace AlianceGuard.Discord
{
    public class DiscordWebhook
    {
        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; } = "AlianceGuard";

        [JsonProperty("avatar_url")]
        public string AvatarUrl { get; set; } = "https://i.imgur.com/zVZv6ar.png";

        [JsonProperty("embeds")]
        public List<DiscordEmbed> Embeds { get; set; } = new List<DiscordEmbed>();
    }
}
