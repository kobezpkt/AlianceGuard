using AlianceGuard.AlianceAPI;
using AlianceGuard.Services;
using AlianceGuard.StringTexts;
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
        public override Version Version => new(1, 0, 1);

        private static readonly HttpClient HttpClient = new();
        private WebhookService _webhookService;
        private UpdateService _updateService;

        public override void OnEnabled()
        {
            InitializationTexts.PrintBanner(Version, Author);

            _webhookService = new WebhookService(HttpClient, Config, Version);
            _updateService = new UpdateService(HttpClient, Version, Config.Debug);

            _updateService.InstallPendingUpdate();

            Exiled.Events.Handlers.Player.Verified += OnPlayerVerified;

            InitializationTexts.ValidateConfiguration(Config);

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
            string apiUrl = $"";
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

            string kickReason = FormattingTexts.FormatKickReason(banInfo);
            player.Kick(kickReason);
        }

        #endregion    
    }
}

