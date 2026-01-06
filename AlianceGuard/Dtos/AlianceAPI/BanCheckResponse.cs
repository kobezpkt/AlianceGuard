using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlianceGuard;

// para deserialização da resposta da API
public class BanCheckResponse
{
    [JsonProperty("isBanned")]
    public bool IsBanned { get; set; }

    [JsonProperty("isAltAccount")]
    public bool IsAltAccount { get; set; }

    [JsonProperty("player")]
    public PlayerBanInfo Player { get; set; }
}
