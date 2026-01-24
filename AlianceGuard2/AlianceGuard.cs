using AlianceGuard.Services;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace AlianceGuard
{
    public class AlianceGuard : Plugin<Config>
    {
        public override string Name => "AlianceGuard";
        public override string Author => "kobezpkt";
        public override Version Version => new Version(1, 1, 0);

        private static readonly HttpClient HttpClient = new HttpClient();
        private WebhookService _webhookService;
        private UpdateService _updateService;

        public override void OnEnabled()
        {
            PrintBanner();

            _webhookService = new WebhookService(HttpClient, Config, Version);
            _updateService = new UpdateService(HttpClient, Version, Config.Debug);

            _updateService.InstallPendingUpdate();

            Exiled.Events.Handlers.Player.Verified += OnPlayerVerified;

            ValidateConfiguration();

            if (Config.CheckForUpdates)
            {
                CheckForUpdatesAsync();
            }

            Log.Info($"{Name} v{Version} habilitado!");
            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.Verified -= OnPlayerVerified;
            _webhookService = null;
            _updateService = null;

            Log.Info($"{Name} v{Version} desabilitado!");
            base.OnDisabled();
        }

        private async void CheckForUpdatesAsync()
        {
            try
            {
                await _updateService.CheckForUpdatesAsync(Config.AutoUpdate);
            }
            catch (Exception ex)
            {
                Log.Error($"Erro ao verificar atualizacoes: {ex.Message}");
            }
        }

        #region Event Handlers

        private async void OnPlayerVerified(VerifiedEventArgs ev)
        {
            try
            {
                await CheckAndKickPlayerAsync(ev.Player);
            }
            catch (Exception ex)
            {
                Log.Error($"Erro ao verificar jogador {ev.Player.Nickname}: {ex}");
            }
        }

        #endregion

        #region Player Verification

        private async Task CheckAndKickPlayerAsync(Player player)
        {
            if (player == null || !player.IsConnected)
                return;

            string steamId = ExtractSteamId(player.UserId);
            if (string.IsNullOrEmpty(steamId))
                return;

            try
            {
                var banInfo = await FetchBanInfoAsync(steamId);

                if (banInfo != null && banInfo.IsBanned)
                {
                    await HandleBannedPlayerAsync(player, banInfo, steamId);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Erro ao verificar jogador na API: {ex.Message}");
            }
        }

        private string ExtractSteamId(string userId)
        {
            string steamId = userId.Replace("@steam", "");
            return steamId.All(char.IsDigit) ? steamId : null;
        }

        private async Task<BanCheckResponse> FetchBanInfoAsync(string steamId)
        {
            string apiUrl = $"https://alianceguard.com/api/exiled/check-player?steamid={steamId}";
            HttpResponseMessage response = await HttpClient.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<BanCheckResponse>(jsonResponse);
            }

            if (Config.Debug)
                Log.Debug($"Erro ao consultar API: {response.StatusCode}");

            return null;
        }

        private async Task HandleBannedPlayerAsync(Player player, BanCheckResponse banInfo, string steamId)
        {
            Log.Warn($"Expulsando jogador banido: {player.Nickname} (SteamID: {steamId})");
            Log.Warn($"Motivo: {banInfo.Player.Reason}");

            await _webhookService.SendBannedPlayerAlertAsync(player, banInfo, steamId);

            string kickReason = FormatKickReason(banInfo);
            player.Kick(kickReason);
        }

        #endregion

        #region Formatting

        private string FormatKickReason(BanCheckResponse banInfo)
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

        private string FormatSeverity(string severity)
        {
            switch (severity)
            {
                case "low":
                    return "Baixa";
                case "medium":
                    return "Media";
                case "high":
                    return "Alta!!";
                case "critical":
                    return "CRITICA!!!";
                default:
                    return "Desconhecida";
            }
        }

        #endregion

        #region Initialization Helpers

        private void PrintBanner()
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
        }

        private void ValidateConfiguration()
        {
            if (Config.WebhookEnabled && string.IsNullOrWhiteSpace(Config.WebhookUrl))
            {
                Log.Warn("Webhook esta habilitada mas a URL nao foi configurada!");
            }
        }

        #endregion
    }
}
