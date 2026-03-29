using AlianceGuard.AlianceAPI;
using AlianceGuard.Services;
using AlianceGuard.StringTexts;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AlianceGuard
{
    public class AlianceGuard : Plugin<Config>
    {
        public override string Name => "AlianceGuard";
        public override string Author => "kobezpkt";
        public override string Prefix => "aliance_guard";
        public override Version Version => new(1, 0, 3);

        private static readonly HttpClient HttpClient = new();
        private WebhookService _webhookService;
        private UpdateService _updateService;
        private ServerHeartbeatService _heartbeatService;
        private PanelAuthService _authService;

        public override void OnEnabled()
        {
            InitializationTexts.PrintBanner(Version, Author);

            _webhookService = new WebhookService(HttpClient, Config, Version);
            _updateService = new UpdateService(HttpClient, Version);
            _heartbeatService = new ServerHeartbeatService(HttpClient, Version);
            _authService = new PanelAuthService(HttpClient, Config, Prefix);

            _updateService.InstallPendingUpdate();

            Exiled.Events.Handlers.Player.Verified += OnPlayerVerified;

            InitializationTexts.ValidateConfiguration(Config);

            _heartbeatService.Start();

            // Autentica com o painel ao iniciar (restaura sessão do config se ainda válida)
            Task.Run(() => _authService.EnsureAuthenticatedAsync());

            if (Config.CheckForUpdates)
                CheckForUpdatesAsync();

            Log.Info($"{Name} v{Version} habilitado");
            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.Verified -= OnPlayerVerified;

            _heartbeatService?.Stop();
            _heartbeatService = null;

            _webhookService = null;
            _updateService = null;
            _authService = null;

            Log.Info($"{Name} v{Version} desabilitado");
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
                Log.Error($"erro ao verificar atualizacoes: {ex.Message}");
            }
        }

        private async void OnPlayerVerified(VerifiedEventArgs ev)
        {
            try
            {
                await CheckAndKickPlayerAsync(ev.Player);
            }
            catch (Exception ex)
            {
                Log.Error($"erro ao verificar jogador {ev.Player.Nickname}: {ex.Message}");
            }
        }

        private async Task CheckAndKickPlayerAsync(Player player)
        {
            if (player == null || !player.IsConnected)
                return;

            string steamId = ExtractSteamId(player.UserId);
            if (string.IsNullOrEmpty(steamId))
                return;

            try
            {
                // Verifica ban geral primeiro
                var banInfo = await FetchBanInfoAsync(steamId);
                if (banInfo != null && (banInfo.IsBanned || banInfo.IsAltAccount))
                {
                    await HandleBannedPlayerAsync(player, banInfo, steamId);
                    return;
                }

                // Registra conexão via endpoint autenticado com HMAC
                var connectionResult = await _authService.RegisterPlayerConnectionAsync(player, steamId);
                if (connectionResult == null)
                    return;

                if (connectionResult.IsBanned)
                {
                    await HandleBannedPlayerAsync(player, banInfo, steamId);
                    return;
                }

                if (connectionResult.AltDetected)
                    await _webhookService.SendAltDetectionAlertAsync(connectionResult);
            }
            catch (Exception ex)
            {
                Log.Error($"erro ao processar jogador {player.Nickname}: {ex.Message}");
            }
        }

        private string ExtractSteamId(string userId)
        {
            string steamId = userId.Replace("@steam", "");
            return long.TryParse(steamId, out _) ? steamId : null;
        }

        private async Task<BanCheckResponse> FetchBanInfoAsync(string steamId)
        {
            string apiUrl = $"https://aliance.owlrpg.com/api/exiled/check-player?steamid={steamId}";
            var response = await HttpClient.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<BanCheckResponse>(json);
            }

            if (Config.Debug)
                Log.Debug($"erro na API Geral: {response.StatusCode}");

            return null;
        }

        private async Task HandleBannedPlayerAsync(Player player, BanCheckResponse banInfo, string steamId)
        {
            await _webhookService.SendBannedPlayerAlertAsync(player, banInfo, steamId);
            string kickReason = FormattingTexts.FormatKickReason(banInfo);
            player.Kick(kickReason);
        }
    }
}