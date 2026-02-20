using Newtonsoft.Json;

namespace AlianceGuard.AlianceAPI.ConnectionResponse;

public class DetectedBannedPlayer
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("username")]
    public string Username { get; set; }

    [JsonProperty("steam_id64")]
    public string SteamId64 { get; set; }
}