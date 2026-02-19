using AlianceGuard.AlianceAPI;
using AlianceGuard.AlianceAPI.ConnectionResponse;
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
        public override Version Version => new(1, 0, 2);

        private static readonly HttpClient HttpClient = new();
        private WebhookService _webhookService;
        private UpdateService _updateService;

        public override void OnEnabled()
        {
            InitializationTexts.PrintBanner(Version, Author);


            _webhookService = new WebhookService(HttpClient, Config, Version);
            _updateService = new UpdateService(HttpClient, Version);


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

        private async Task CheckAndKickPlayerAsync(Player player)
        {
            if (player == null || !player.IsConnected)
                return;

            string steamId = ExtractSteamId(player.UserId);
            if (string.IsNullOrEmpty(steamId))
                return;

            try
            {
                var connectionResult = await RegisterPlayerConnectionAsync(player, steamId);
                var banInfo = await FetchBanInfoAsync(steamId);

                if (connectionResult != null)
                {
                    if (!connectionResult.IsBanned && connectionResult.AltDetected)
                    {
                        await _webhookService.SendAltDetectionAlertAsync(connectionResult);
                    }
                }
                else
                {
                    if (banInfo != null && banInfo.IsBanned)
                    {
                        await HandleBannedPlayerAsync(player, banInfo, steamId);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Erro ao verificar jogador na API: {ex.Message}");
            }
        }

        private async Task<PlayerConnectionResponse> RegisterPlayerConnectionAsync(Player player, string steamId)
        {
            try
            {
                // api das alt's
                string apiUrl = $"";

                var payload = new
                {
                    steam_id64 = steamId,
                    player_name = player.Nickname,
                    ip_address = player.IPAddress
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(payload),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                /// Arruma isso aqui, nao tem nem response kkkkk

                //if (Config.Debug)
                //    Log.Debug($"Erro ao registrar conexão na API: {response.StatusCode}");

                return null;
            }
            catch (Exception ex)
            {
                Log.Error($"Erro ao registrar conexão: {ex.Message}");
                return null;
            }
        }

        private string ExtractSteamId(string userId)
        {
            string steamId = userId.Replace("@steam", "");
            return steamId.All(char.IsDigit) ? steamId : null;
        }

        private async Task<BanCheckResponse> FetchBanInfoAsync(string steamId)
        {
            // api geral
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
    }
}
