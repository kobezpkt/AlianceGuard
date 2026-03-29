using AlianceGuard.AlianceAPI.ConnectionResponse;
using Exiled.API.Features;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AlianceGuard.Services
{
    /// <summary>
    /// Gerencia a autenticação HMAC com o painel AlianceGuard.
    /// Obtém account_id via "!id" e server_ip via ServerConsole.Ip.
    /// Obtém api_key via "!api reset" apenas uma vez e persiste no config.yml.
    /// </summary>
    public class PanelAuthService
    {
        // URL do painel — não exposta no config
        private const string PanelUrl = "https://aliance.owlrpg.com/";

        private readonly HttpClient _http;
        private readonly Config _config;
        private readonly string _prefix;

        private string _hmacKey = "";
        private string _serverId = "";
        private DateTime _expiresAt = DateTime.MinValue;

        private string _accountId = "";
        private string _serverIp = "";

        private static readonly SemaphoreSlim AuthLock = new(1, 1);

        public PanelAuthService(HttpClient http, Config config, string prefix)
        {
            _http = http;
            _config = config;
            _prefix = prefix;

            LoadSessionFromConfig();
        }

        // -------------------------------------------------------------------------
        // Restaura sessão do config.yml se ainda estiver válida
        // -------------------------------------------------------------------------
        private void LoadSessionFromConfig()
        {
            if (string.IsNullOrEmpty(_config.SessionHmacKey) ||
                string.IsNullOrEmpty(_config.SessionServerId) ||
                string.IsNullOrEmpty(_config.SessionExpiresAt))
                return;

            if (!DateTime.TryParse(_config.SessionExpiresAt, out var exp))
                return;

            if (exp <= DateTime.UtcNow.AddMinutes(10))
                return;

            _hmacKey = _config.SessionHmacKey;
            _serverId = _config.SessionServerId;
            _expiresAt = exp;

            Log.Info($"[AlianceGuard] Sessao restaurada do config. Expira em: {_expiresAt:u}");
        }

        // -------------------------------------------------------------------------
        // Executa um comando via GameCore.Console.Singleton.TypeCommand
        // Retorna o output diretamente como string
        // -------------------------------------------------------------------------
        private static string RunCommand(string command)
            => GameCore.Console.Singleton.TypeCommand(command);

        // -------------------------------------------------------------------------
        // Obtém server_ip via ServerConsole.Ip e account_id via "!id"
        // -------------------------------------------------------------------------
        private bool FetchServerInfo()
        {
            try
            {
                // IP disponível diretamente na propriedade estática
                _serverIp = ServerConsole.Ip ?? "";

                // account_id extraído do output de "!id"
                var output = RunCommand("!id");

                var match = Regex.Match(output ?? "", @"Account\s*ID[:\s]+(\d+)", RegexOptions.IgnoreCase);
                if (match.Success)
                    _accountId = match.Groups[1].Value.Trim();

                // Fallback: tenta extrair de "Your ID is XXXX"
                if (string.IsNullOrEmpty(_accountId))
                {
                    var fallback = Regex.Match(output ?? "", @"(?:your\s+id\s+is|id[:\s]+)\s*(\d+)", RegexOptions.IgnoreCase);
                    if (fallback.Success)
                        _accountId = fallback.Groups[1].Value.Trim();
                }

                if (string.IsNullOrEmpty(_serverIp))
                {
                    Log.Error("[AlianceGuard] Nao foi possivel obter server_ip via ServerConsole.Ip.");
                    return false;
                }

                if (string.IsNullOrEmpty(_accountId))
                {
                    Log.Error($"[AlianceGuard] Nao foi possivel extrair account_id do output de '!id': {output}");
                    return false;
                }

                Log.Info($"[AlianceGuard] account_id={_accountId} | server_ip={_serverIp}");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[AlianceGuard] Erro ao obter informacoes do servidor: {ex.Message}");
                return false;
            }
        }

        // -------------------------------------------------------------------------
        // Obtém api_key via "!api reset" — apenas se não houver uma salva
        // -------------------------------------------------------------------------
        private bool EnsureApiKey()
        {
            if (!string.IsNullOrEmpty(_config.ApiKey))
            {
                Log.Info("[AlianceGuard] Usando API Key salva no config.");
                return true;
            }

            Log.Info("[AlianceGuard] Gerando nova API Key via '!api reset'...");

            var output = RunCommand("!api reset");

            var match = Regex.Match(output ?? "", @"(?:new\s+api\s+key|api\s+key)[:\s]+([A-Za-z0-9\-_]+)", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                Log.Error($"[AlianceGuard] Nao foi possivel extrair API Key do output: {output}");
                return false;
            }

            _config.ApiKey = match.Groups[1].Value.Trim();
            SaveConfig();

            Log.Info("[AlianceGuard] API Key obtida e salva no config.");
            return true;
        }

        // -------------------------------------------------------------------------
        // Garante sessão ativa — autentica com NW apenas quando necessário
        // -------------------------------------------------------------------------
        public async Task EnsureAuthenticatedAsync()
        {
            if (_expiresAt > DateTime.UtcNow.AddMinutes(10) && !string.IsNullOrEmpty(_hmacKey))
                return;

            await AuthLock.WaitAsync();
            try
            {
                if (_expiresAt > DateTime.UtcNow.AddMinutes(10) && !string.IsNullOrEmpty(_hmacKey))
                    return;

                if (!FetchServerInfo()) return;
                if (!EnsureApiKey()) return;

                Log.Info("[AlianceGuard] Autenticando com o painel...");

                var payload = JsonConvert.SerializeObject(new
                {
                    account_id = _accountId,
                    api_key = _config.ApiKey,
                    server_ip = _serverIp,
                });

                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                var response = await _http.PostAsync($"{PanelUrl}/api/exiled/auth", content);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Log.Error($"[AlianceGuard] Falha na autenticacao: {response.StatusCode} — {body}");

                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        _config.ApiKey = "";
                        SaveConfig();
                    }
                    return;
                }

                dynamic json = JsonConvert.DeserializeObject(body)!;
                _hmacKey = (string)json.hmac_key;
                _serverId = ((int)json.server_id).ToString();
                _expiresAt = DateTime.Parse((string)json.expires_at);

                _config.SessionHmacKey = _hmacKey;
                _config.SessionServerId = _serverId;
                _config.SessionExpiresAt = _expiresAt.ToString("o");
                SaveConfig();

                Log.Info($"[AlianceGuard] Autenticado! server_id={_serverId} | expira em {_expiresAt:u}");
            }
            catch (Exception ex)
            {
                Log.Error($"[AlianceGuard] Erro ao autenticar: {ex.Message}");
            }
            finally
            {
                AuthLock.Release();
            }
        }

        // -------------------------------------------------------------------------
        // Registra conexão do jogador no painel com assinatura HMAC
        // -------------------------------------------------------------------------
        public async Task<PlayerConnectionResponse> RegisterPlayerConnectionAsync(
            Exiled.API.Features.Player player, string steamId)
        {
            await EnsureAuthenticatedAsync();

            if (string.IsNullOrEmpty(_hmacKey))
            {
                Log.Warn("[AlianceGuard] Sem sessao ativa — nao foi possivel verificar o jogador.");
                return null;
            }

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var payload = JsonConvert.SerializeObject(new
            {
                steam_id64 = steamId,
                player_name = player.Nickname,
                ip_address = player.IPAddress,
                timestamp,
            });

            var signature = ComputeHmac(payload);

            var request = new HttpRequestMessage(HttpMethod.Post, $"{PanelUrl}/api/exiled/player-connect")
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
            };
            request.Headers.Add("X-Server-Id", _serverId);
            request.Headers.Add("X-Plugin-Signature", signature);

            var response = await _http.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                dynamic errJson = JsonConvert.DeserializeObject(body)!;
                if ((bool?)errJson?.reauth_required == true)
                {
                    Log.Info("[AlianceGuard] Sessao expirada — reautenticando...");
                    _expiresAt = DateTime.MinValue;
                    await EnsureAuthenticatedAsync();
                    return await RegisterPlayerConnectionAsync(player, steamId);
                }
            }

            if (!response.IsSuccessStatusCode)
            {
                if (_config.Debug)
                    Log.Debug($"[AlianceGuard] Erro ao registrar jogador {player.Nickname}: {response.StatusCode}");
                return null;
            }

            return JsonConvert.DeserializeObject<PlayerConnectionResponse>(body);
        }

        // -------------------------------------------------------------------------
        // Calcula HMAC-SHA256 do body com a chave de sessão atual
        // -------------------------------------------------------------------------
        private string ComputeHmac(string body)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_hmacKey));
            return BitConverter.ToString(hmac.ComputeHash(Encoding.UTF8.GetBytes(body))).Replace("-", "").ToLower();
        }

        // -------------------------------------------------------------------------
        // Salva config.yml com sessão e api_key atualizadas
        // -------------------------------------------------------------------------
        private void SaveConfig()
        {
            try
            {
                var configPath = Path.Combine(Paths.Configs, $"{_prefix}.yml");
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .Build();
                File.WriteAllText(configPath, serializer.Serialize(_config));
            }
            catch (Exception ex)
            {
                Log.Warn($"[AlianceGuard] Nao foi possivel salvar config.yml: {ex.Message}");
            }
        }
    }
}
