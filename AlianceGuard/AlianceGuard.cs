using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using Newtonsoft.Json;

namespace AlianceGuard;

public class AlianceGuard : Plugin<Config>
{
    public override string Name => "AlianceGuard";
    public override string Author => "kobezpkt";
    public override Version Version => new(1, 0, 0);
    public override Version RequiredExiledVersion => new(8, 0, 0);

    private static readonly HttpClient httpClient = new();

    public override void OnEnabled()
    {
        Log.Info("█████╗ ██╗     ██╗ ██████╗███╗   ██╗ ██████╗███████╗");
        Log.Info("██╔══██╗██║     ██║██╔════╝████╗  ██║██╔════╝██╔════╝");
        Log.Info("███████║██║     ██║███████╗██╔██╗ ██║██║     █████╗  ");
        Log.Info("██╔══██║██║     ██║██╔═══██║██║╚██╗██║██║     ██╔══╝  ");
        Log.Info("██║  ██║███████╗██║╚██████╔╝██║ ╚████║╚██████╗███████╗");
        Log.Info("╚═╝  ╚═╝╚══════╝╚═╝ ╚═════╝ ╚═╝  ╚═══╝ ╚═════╝╚══════╝");
        Log.Info("");
        Log.Info($"AlianceGuard v{Version} by {Author}");
        Log.Info("");

        Exiled.Events.Handlers.Player.Verified += OnPlayerVerified;

        Log.Info($"{Name} v{Version} habilitado!");
        base.OnEnabled();
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.Verified -= OnPlayerVerified;

        Log.Info($"{Name} v{Version} desabilitado!");
        base.OnDisabled();
    }

    private async void OnPlayerVerified(VerifiedEventArgs ev)
    {
        try
        {
            await CheckAndKickPlayer(ev.Player);
        }
        catch (Exception ex)
        {
            Log.Error($"Erro ao verificar jogador {ev.Player.Nickname}: {ex}");
        }
    }

    // Verifica o player ta na API 
    private async Task CheckAndKickPlayer(Player player)
    {
        if (player == null || !player.IsConnected)
            return;

        string steamId = player.UserId.Replace("@steam", "");

        // ignora se n for ID válido
        if (!steamId.All(char.IsDigit))
            return;

        try
        {
            string apiUrl = $"";
            HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var banInfo = JsonConvert.DeserializeObject<BanCheckResponse>(jsonResponse);

                if (banInfo != null && banInfo.IsBanned)
                {
                    string kickReason = FormatKickReason(banInfo);

                    Log.Warn($"Expulsando jogador banido: {player.Nickname} (SteamID: {steamId})");
                    Log.Warn($"Motivo: {banInfo.Player.Reason}");

                    player.Kick(kickReason);
                }
            }
            else
            {
                Log.Error($"Erro ao consultar API: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Erro ao verificar jogador na API: {ex.Message}");
        }
    }

    // mensagem de banimento com cores
    private string FormatKickReason(BanCheckResponse banInfo)
    {
        string altAccountText = banInfo.IsAltAccount ? " (Conta Alternativa)" : "";

        return $"<color=white><b>.</b></color>\n\n" +
               $"<color=red><b>AlianceGuard</b></color>\n\n" +
               $"<color=red>Seu SteamID64 foi encontrado em nossa Database com uma violação extremamente seria.</color>\n\n" +
               $"<color=white>Jogador:</color> <color=white>{banInfo.Player.Username}</color>\n" +
               $"<color=white>Steam ID:</color> <color=white>{banInfo.Player.SteamId}</color>{altAccountText}\n\n" +
               $"<color=red>MOTIVO:</color>\n" +
               $"<color=white>{banInfo.Player.Reason}</color>\n\n" +
               $"<color=yellow>Severidade:</color> <color=white>{GetSeverityText(banInfo.Player.Severity)}</color>\n" +
               $"<color=yellow>Adicionado por:</color> <color=white>{banInfo.Player.AddedBy}</color>\n\n" +
               $"<color=yellow>Caso ache que isso e um erro, entre em contato com o nosso suporte no discord:</color>\n\n" +
               $"<color=white>https://discord.gg/eA8JusX8tq</color>\n\n";
    }

    private string GetSeverityText(string severity)
    {
        return severity switch
        {
            "low" => "Baixa",
            "medium" => "Média",
            "high" => "Alta!!",
            "critical" => "CRÍTICA!!!",
            _ => "Desconhecida",
        };
    }
}
