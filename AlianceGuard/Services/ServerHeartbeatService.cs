using Exiled.API.Features;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AlianceGuard.Services
{
    public class ServerHeartbeatService
    {
        private readonly HttpClient _httpClient;
        private readonly Version _pluginVersion;
        private Timer _heartbeatTimer;
        private const int HeartbeatIntervalMinutes = 3;

        public ServerHeartbeatService(HttpClient httpClient, Version pluginVersion)
        {
            _httpClient = httpClient;
            _pluginVersion = pluginVersion;
        }

        public void Start()
        {
            // enviar informações ao iniciar
            Task.Run(SendHeartbeat);

            // enviar a cada 3 min
            _heartbeatTimer = new Timer(
                async _ => await SendHeartbeat(),
                null,
                TimeSpan.FromMinutes(HeartbeatIntervalMinutes),
                TimeSpan.FromMinutes(HeartbeatIntervalMinutes)
            );
        }

        public void Stop()
        {
            _heartbeatTimer?.Dispose();
            _heartbeatTimer = null;
        }

        private async Task SendHeartbeat()
        {
            try
            {
                // pegar informações do servidor
                string serverName = CleanServerName(Server.Name ?? "Unknown Server");
                string serverIp = await GetPublicIpAsync();
                ushort serverPort = Server.Port;
                int playerCount = Player.List.Count();
                string pluginVersion = _pluginVersion.ToString();

                if (string.IsNullOrEmpty(serverIp))
                {
                    Log.Warn("[AlianceGuard] IP não obtido");
                    return;
                }

                string apiUrl = $"";

                var payload = new
                {
                    server_name = serverName,
                    server_ip = serverIp,
                    server_port = serverPort,
                    current_players = playerCount,
                    plugin_version = pluginVersion
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(payload),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    Log.Debug($"[AlianceGuard] enviando: {serverName} ({serverIp}:{serverPort}) - {playerCount} jogadores");
                }
                else
                {
                    Log.Warn($"[AlianceGuard] falha: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[AlianceGuard] erro: {ex.Message}");
            }
        }

        private async Task<string> GetPublicIpAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("https://api.ipify.org");
                if (response.IsSuccessStatusCode)
                {
                    return (await response.Content.ReadAsStringAsync()).Trim();
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"[AlianceGuard] erro ao pegar o IP: {ex.Message}");
            }

            return null;
        }

        private string CleanServerName(string rawName)
        {
            if (string.IsNullOrEmpty(rawName))
                return "Unknown Server";
            string cleaned = Regex.Replace(rawName, @"<color[^>]*>|</color>|<b>|</b>|<size[^>]*>|</size>", "");
            cleaned = Regex.Replace(cleaned, @"\{[^}]*\}", "");
            cleaned = cleaned.Replace("\\n", " ").Replace("\n", " ");
            cleaned = Regex.Replace(cleaned, @"\s+", " ");
            if (cleaned.Contains("|"))
            {
                cleaned = cleaned.Substring(0, cleaned.IndexOf('|'));
            }
            cleaned = cleaned.Trim();

            return string.IsNullOrEmpty(cleaned) ? "Unknown Server" : cleaned;
        }
    }
}
