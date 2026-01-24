using Newtonsoft.Json;

namespace AlianceGuard
{
    public class PlayerBanInfo
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("steamId")]
        public string SteamId { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("reason")]
        public string Reason { get; set; }

        [JsonProperty("severity")]
        public string Severity { get; set; }

        [JsonProperty("addedBy")]
        public string AddedBy { get; set; }
    }
}
