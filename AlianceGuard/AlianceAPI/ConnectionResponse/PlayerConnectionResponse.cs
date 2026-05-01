using AlianceGuard.AlianceAPI.ConnectionResponse;
using Newtonsoft.Json;

namespace AlianceGuard.AlianceAPI.ConnectionResponse;

public class PlayerConnectionResponse
{
    [JsonProperty("isBanned")]
    public bool IsBanned { get; set; }

    [JsonProperty("role_ingame")]
    public string RoleIngame { get; set; }

    [JsonProperty("role_color")]
    public string RoleColor { get; set; }

    [JsonProperty("alt_detected")]
    public bool AltDetected { get; set; }

    [JsonProperty("should_kick")]
    public bool ShouldKick { get; set; }

    [JsonProperty("detection_uuid")]
    public string DetectionUuid { get; set; }

    [JsonProperty("banned_player")]
    public DetectedBannedPlayer BannedPlayer { get; set; }

    [JsonProperty("detected_player")]
    public DetectedAltPlayer DetectedPlayer { get; set; }

    [JsonProperty("detection_type")]
    public string DetectionType { get; set; }
}
