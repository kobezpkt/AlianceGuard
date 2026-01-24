using Newtonsoft.Json;
using System.Collections.Generic;

namespace AlianceGuard.Dtos.Discord
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

    public class DiscordEmbedField
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("inline")]
        public bool Inline { get; set; }
    }

    public class DiscordEmbedFooter
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("icon_url")]
        public string IconUrl { get; set; }
    }

    public class DiscordEmbedThumbnail
    {
        [JsonProperty("url")]
        public string Url { get; set; }
    }
}
