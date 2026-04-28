using AlianceGuard.AlianceAPI;

namespace AlianceGuard.StringTexts;

public class FormattingTexts
{
    public static string FormatKickReason(BanCheckResponse banInfo)
    {
        string altAccountText = banInfo.IsAltAccount ? " (Conta Alternativa)" : "";

        return $"<color=white><b>.</b></color>\n\n" +
               $"<color=red><b>AlianceGuard</b></color>\n" +
               $"<color=red>Seu SteamID64 foi encontrado em nossa Database com uma violacao extremamente seria.</color>\n" +
               $"<color=white>Jogador:</color> <color=white>{banInfo.Player.Username}</color>\n" +
               $"<color=white>Steam ID:</color> <color=white>{banInfo.Player.SteamId}</color>{altAccountText}\n" +
               $"<color=red>MOTIVO:</color>\n" +
               $"<color=white>{banInfo.Player.Reason}</color>\n" +
               $"<color=yellow>Severidade:</color> <color=white>{FormatSeverity(banInfo.Player.Severity)}</color>\n" +
               $"<color=yellow>Adicionado por:</color> <color=white>{banInfo.Player.AddedBy}</color>\n" +
               $"<color=yellow>Caso ache que isso e um erro, entre em contato com o nosso suporte no discord:</color>\n" +
               $"<color=white>https://discord.gg/eA8JusX8tq</color>\n";
    }

    private static string FormatSeverity(string severity)
    {
        return severity switch
        {
            "low" => "Baixa",
            "medium" => "Media",
            "high" => "Alta!!",
            "critical" => "CRITICA!!!",
            _ => "Desconhecida",
        };
    }
}