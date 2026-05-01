using Newtonsoft.Json;

namespace AlianceGuard.AlianceAPI;

public class BanCheckResponse
{
    [JsonProperty("isBanned")]
    public bool IsBanned { get; set; }

    [JsonProperty("isAltAccount")]
    public bool IsAltAccount { get; set; }

    [JsonProperty("player")]
    public PlayerBanInfo Player { get; set; }

    [JsonProperty("role_ingame")]
    public string RoleIngame { get; set; }

    [JsonProperty("role_color")]
    public string RoleColor { get; set; }
}
