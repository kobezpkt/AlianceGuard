using Exiled.API.Features;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace AlianceGuard.Services;

public class UpdateService
{
    private readonly HttpClient _httpClient;
    private readonly Version _currentVersion;

    private const string GitHubApiUrl = "https://api.github.com/repos/kobezpkt/AlianceGuard/releases/latest";
    private const string PluginFileName = "AlianceGuard.dll";

    public UpdateService(HttpClient httpClient, Version currentVersion)
    {
        _httpClient = httpClient;
        _currentVersion = currentVersion;

        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AlianceGuard-Plugin");
        }
    }

    public async Task CheckForUpdatesAsync(bool autoUpdate)
    {
        try
        {
            Log.Debug("Verificando atualizacoes no GitHub...");

            var response = await _httpClient.GetAsync(GitHubApiUrl);

            if (!response.IsSuccessStatusCode)
            {
                Log.Error($"Erro ao verificar atualizacoes: {response.StatusCode}");
                return;
            }

            string jsonResponse = await response.Content.ReadAsStringAsync();
            var release = JsonConvert.DeserializeObject<GitHubRelease>(jsonResponse);

            if (release == null || string.IsNullOrEmpty(release.TagName))
            {
                Log.Error("Nenhuma release encontrada no GitHub");
                return;
            }

            string versionString = release.TagName.TrimStart('v', 'V');

            if (!Version.TryParse(versionString, out Version latestVersion))
            {
                Log.Error($"Nao foi possivel parsear a versao: {release.TagName}");
                return;
            }

            int comparison = latestVersion.CompareTo(_currentVersion);

            if (comparison > 0)
            {
                Log.Warn("═══════════════════════════════════════════════════════════");
                Log.Warn($"  NOVA VERSAO DISPONIVEL: v{latestVersion}");
                Log.Warn($"  Versao atual: v{_currentVersion}");
                Log.Warn($"  Changelog: {release.HtmlUrl}");
                Log.Warn("═══════════════════════════════════════════════════════════");

                if (autoUpdate)
                {
                    await DownloadAndInstallUpdateAsync(release, latestVersion);
                }
                else
                {
                    Log.Warn("Auto-atualizacao desabilitada. Ative 'AutoUpdate' no config para atualizar automaticamente.");
                    Log.Warn($"Ou baixe manualmente em: {release.HtmlUrl}");
                }
            }
            else if (comparison == 0)
            {
                Log.Info($"AlianceGuard esta atualizado! (v{_currentVersion})");
            }
            else
            {
                Log.Info($"Versao atual (v{_currentVersion}) e mais recente que a do GitHub (v{latestVersion})");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Erro ao verificar atualizacoes: {ex.Message}");
            Log.Debug($"Stack trace: {ex.StackTrace}");
        }
    }

    private async Task DownloadAndInstallUpdateAsync(GitHubRelease release, Version newVersion)
    {
        try
        {
            GitHubAsset dllAsset = null;

            if (release.Assets != null)
            {
                foreach (var asset in release.Assets)
                {
                    if (asset.Name != null && asset.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    {
                        dllAsset = asset;
                        break;
                    }
                }
            }

            if (dllAsset == null)
            {
                Log.Warn("Nenhum arquivo .dll encontrado na release. Baixe manualmente.");
                Log.Warn($"Link: {release.HtmlUrl}");
                return;
            }

            Log.Info($"Baixando atualizacao v{newVersion}...");

            var downloadResponse = await _httpClient.GetAsync(dllAsset.BrowserDownloadUrl);

            if (!downloadResponse.IsSuccessStatusCode)
            {
                Log.Error($"Erro ao baixar atualizacao: {downloadResponse.StatusCode}");
                return;
            }

            byte[] fileBytes = await downloadResponse.Content.ReadAsByteArrayAsync();

            string pluginPath = Path.Combine(Paths.Plugins, PluginFileName);
            string backupPath = Path.Combine(Paths.Plugins, $"AlianceGuard.v{_currentVersion}.backup.dll");
            string pendingPath = Path.Combine(Paths.Plugins, "AlianceGuard.pending.dll");

            if (File.Exists(pluginPath))
            {
                try
                {
                    File.Copy(pluginPath, backupPath, true);
                    Log.Info($"Backup criado: {backupPath}");
                }
                catch (Exception ex)
                {
                    Log.Warn($"Nao foi possivel criar backup: {ex.Message}");
                }
            }

            File.WriteAllBytes(pendingPath, fileBytes);

            try
            {
                File.Copy(pendingPath, pluginPath, true);
                File.Delete(pendingPath);

                Log.Info("═══════════════════════════════════════════════════════════");
                Log.Info($"  ATUALIZACAO v{newVersion} INSTALADA COM SUCESSO!");
                Log.Info("  Reinicie o servidor para aplicar a atualizacao.");
                Log.Info("═══════════════════════════════════════════════════════════");
            }
            catch (IOException)
            {
                // Arquivo em uso - sera substituido no proximo reinicio
                Log.Info("═══════════════════════════════════════════════════════════");
                Log.Info($"  ATUALIZACAO v{newVersion} BAIXADA!");
                Log.Info("  O arquivo sera atualizado no proximo reinicio do servidor.");
                Log.Info($"  Arquivo pendente: {pendingPath}");
                Log.Info("═══════════════════════════════════════════════════════════");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Erro ao instalar atualizacao: {ex.Message}");
            Log.Debug($"Stack trace: {ex.StackTrace}");
        }
    }

    public void InstallPendingUpdate()
    {
        try
        {
            string pluginPath = Path.Combine(Paths.Plugins, PluginFileName);
            string pendingPath = Path.Combine(Paths.Plugins, "AlianceGuard.pending.dll");

            if (File.Exists(pendingPath))
            {
                Log.Info("Atualizacao pendente encontrada. Instalando...");

                File.Copy(pendingPath, pluginPath, true);
                File.Delete(pendingPath);

                Log.Info("Atualizacao instalada com sucesso!");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Erro ao instalar atualizacao pendente: {ex.Message}");
        }
    }
}

#region GitHub API Models

public class GitHubRelease
{
    [JsonProperty("tag_name")]
    public string TagName { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("html_url")]
    public string HtmlUrl { get; set; }

    [JsonProperty("body")]
    public string Body { get; set; }

    [JsonProperty("assets")]
    public GitHubAsset[] Assets { get; set; }

    [JsonProperty("published_at")]
    public DateTime PublishedAt { get; set; }
}

public class GitHubAsset
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("browser_download_url")]
    public string BrowserDownloadUrl { get; set; }

    [JsonProperty("size")]
    public long Size { get; set; }
}

#endregion
