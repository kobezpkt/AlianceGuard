using Newtonsoft.Json;

namespace AlianceGuard.AlianceAPI.ConnectionResponse;

public class DetectedAltPlayer
{
    [JsonProperty("steam_id64")]
    public string SteamId64 { get; set; }

    [JsonProperty("player_name")]
    public string PlayerName { get; set; }
}
