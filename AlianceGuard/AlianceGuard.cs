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

namespace AlianceGuard;

public class AlianceGuard : Plugin<Config>
{
    public override string Name => "AlianceGuard";
    public override string Author => "kobezpkt";
    public override Version Version => new(1, 0, 3);

    private static readonly HttpClient HttpClient = new();
    private WebhookService _webhookService;
    private UpdateService _updateService;
    private ServerHeartbeatService _heartbeatService;
    private RoleAssignmentService _roleAssignmentService;

    public override void OnEnabled()
    {
        InitializationTexts.PrintBanner(Version, Author);

        _webhookService = new WebhookService(HttpClient, Config, Version);
        _updateService = new UpdateService(HttpClient, Version);
        _heartbeatService = new ServerHeartbeatService(HttpClient, Version);
        _roleAssignmentService = new RoleAssignmentService();

        _updateService.InstallPendingUpdate();

        Exiled.Events.Handlers.Player.Verified += OnPlayerVerified;

        InitializationTexts.ValidateConfiguration(Config);

        _heartbeatService.Start();

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
        _roleAssignmentService = null;

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
            await CheckPlayerAsync(ev.Player);
        }
        catch (Exception ex)
        {
            Log.Error($"[AlianceGuard] erro ao verificar jogador {ev.Player.Nickname}: {ex}");
        }
    }

    private async Task CheckPlayerAsync(Player player)
    {
        if (player == null || !player.IsConnected)       
            return;

        player.Broadcast(1, "<b>Este Servidor usa AlianceGuard!</b>");

        string steamId = ExtractSteamId(player.UserId);

        if (string.IsNullOrEmpty(steamId))
            return;      

        try
        {
            var banInfo = await FetchBanInfoAsync(steamId);

            if (banInfo == null)
                return;            

            if (banInfo.IsBanned || banInfo.IsAltAccount)
            {
                await HandleBannedPlayerAsync(player, banInfo, steamId);
                return;
            }

            var connectionResult = await RegisterPlayerConnectionAsync(player, steamId);

            if (connectionResult != null)
            {         
                if (connectionResult.IsBanned || connectionResult.ShouldKick)
                {
                    await HandleBannedPlayerAsync(player, banInfo, steamId);
                    return;
                }

                if (connectionResult.AltDetected)
                {
                    await _webhookService.SendAltDetectionAlertAsync(connectionResult);
                }
            }

            if (!string.IsNullOrWhiteSpace(banInfo.RoleIngame))
            {
                _roleAssignmentService.AssignRole(player, banInfo.RoleIngame, banInfo.RoleColor, Config);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[AlianceGuard] erro ao processar jogador {player.Nickname}: {ex.Message}");
            Log.Error($"[AlianceGuard] StackTrace: {ex.StackTrace}");
        }
    }

    private async Task<PlayerConnectionResponse> RegisterPlayerConnectionAsync(Player player, string steamId)
    {
        try
        {
            string apiUrl = "";

            var payload = new
            {
                steam_id64 = steamId,
                player_name = player.Nickname,
                ip_address = player.IPAddress
            };

            string payloadJson = JsonConvert.SerializeObject(payload);

            var content = new StringContent(payloadJson, System.Text.Encoding.UTF8, "application/json");
            var response = await HttpClient.PostAsync(apiUrl, content);

            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<PlayerConnectionResponse>(jsonResponse);
            }

            return null;
        }
        catch (Exception ex)
        {
            Log.Error($"[AlianceGuard] [4.E] Excecao em RegisterPlayerConnectionAsync: {ex.Message}");
            return null;
        }
    }

    private string ExtractSteamId(string userId)
    {
        string steamId = userId?.Replace("@steam", "");
        return steamId != null && steamId.All(char.IsDigit) ? steamId : null;
    }

    private async Task<BanCheckResponse> FetchBanInfoAsync(string steamId)
    {
        try
        {
            string apiUrl = $"";
            HttpResponseMessage response = await HttpClient.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<BanCheckResponse>(jsonResponse);  
                return result;
            }

            return null;
        }
        catch (Exception ex)
        {
            Log.Error($"[AlianceGuard] [3.F] Excecao em FetchBanInfoAsync: {ex.Message}");
            return null;
        }
    }

    private async Task HandleBannedPlayerAsync(Player player, BanCheckResponse banInfo, string steamId)
    {
        await _webhookService.SendBannedPlayerAlertAsync(player, banInfo, steamId);
        string kickReason = FormattingTexts.FormatKickReason(banInfo);
        player.Kick(kickReason);
    }
}
