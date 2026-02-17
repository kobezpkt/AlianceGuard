using AlianceGuard.AlianceAPI;
using Exiled.API.Features;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AlianceGuard.Services;

/// serviço q envia notificações via webhook (pfv n mexer pq fiquei 2h so arrumando isso)
/// Arrumei, pega essa - MMDDKK
/// Tira o MMDDKK como dev esse mano n sabe deixar bonitinho as coisas (contem ironia)
public class WebhookService(HttpClient httpClient, Config config, Version pluginVersion)
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly Config _config = config;
    private readonly Version _pluginVersion = pluginVersion;

    public async Task SendBannedPlayerAlertAsync(Player player, BanCheckResponse banInfo, string steamId)
    {
        if (!_config.WebhookEnabled || string.IsNullOrWhiteSpace(_config.WebhookUrl))
            return;

        if (!ValidateWebhookUrl(_config.WebhookUrl))
        {
            LogDebug("URL da webhook invalida ou nao e uma webhook do Discord");
            return;
        }

        try
        {
            var webhook = BuildBannedPlayerWebhook(player, banInfo, steamId);
            await SendWebhookAsync(webhook);

            LogDebug($"Webhook enviada com sucesso para jogador banido: {player.Nickname}");
        }
        catch (Exception ex)
        {
            LogDebug($"Erro ao enviar webhook: {ex.Message}");
        }
    }
    private bool ValidateWebhookUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri webhookUri))
            return false;

        return webhookUri.Host.EndsWith("discord.com") ||
               webhookUri.Host.EndsWith("discordapp.com");
    }

    private Discord.DiscordWebhook BuildBannedPlayerWebhook(Player player, BanCheckResponse banInfo, string steamId)
    {
        int embedColor = ParseEmbedColor(_config.WebhookEmbedColor);
        string accountType = banInfo.IsAltAccount ? "Conta Alternativa (ALT)" : "Conta Principal";
        string accountEmoji = banInfo.IsAltAccount ? ":warning:" : ":bust_in_silhouette:";

        var fields = new List<Discord.DiscordEmbedField>
        {
            CreateField(":video_game: Nome no Jogo", $"`{player.Nickname}`", true),
            CreateField(":label: Username Steam", $"`{banInfo.Player.Username}`", true),
            CreateField(":id: SteamID64", $"`{steamId}`", true),
            CreateField($"{accountEmoji} Tipo de Conta", $"**{accountType}**", true),
            CreateField(":scales: Severidade", $"`{FormatSeverity(banInfo.Player.Severity)}`", true),
            CreateField(":pencil: Adicionado por", $"`{banInfo.Player.AddedBy}`", true),
            CreateField(":page_facing_up: Motivo do Registro", $"```{banInfo.Player.Reason}```", false),
            CreateField(":link: Links Uteis", $"[Steam Profile](https://steamcommunity.com/profiles/{steamId}) | [SteamID.io](https://steamid.io/lookup/{steamId})", false)
        };

        var embed = new Discord.DiscordEmbed
        {
            Title = ":rotating_light: Ban alert!",
            Description = $"Um jogador registrado na database do AlianceGuard tentou entrar no servidor.",
            Color = embedColor,
            Fields = fields,
            Footer = new Discord.DiscordEmbedFooter
            {
                Text = $"AlianceGuard v{_pluginVersion}",
                IconUrl = "https://i.imgur.com/zVZv6ar.png"
            },
            Timestamp = DateTime.UtcNow.ToString("o"),
            Thumbnail = new Discord.DiscordEmbedThumbnail
            {
                Url = $"https://avatars.cloudflare.steamstatic.com/{steamId}_full.jpg"
            }
        };

        return new Discord.DiscordWebhook
        {
            Content = string.IsNullOrWhiteSpace(_config.WebhookMentionRole) ? null : _config.WebhookMentionRole,
            Embeds = [embed]
        };
    }

    private async Task SendWebhookAsync(Discord.DiscordWebhook webhook)
    {
        string jsonPayload = JsonConvert.SerializeObject(webhook, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        });

        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(_config.WebhookUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            string errorContent = await response.Content.ReadAsStringAsync();
            LogDebug($"Erro ao enviar webhook: {response.StatusCode} - {errorContent}");
        }
    }

    private int ParseEmbedColor(string hexColor)
    {
        int defaultColor = 0xFF0000;

        if (string.IsNullOrWhiteSpace(hexColor))
            return defaultColor;

        try
        {
            return int.Parse(hexColor.Replace("#", ""), NumberStyles.HexNumber);
        }
        catch
        {
            LogDebug($"Cor invalida: {hexColor}, usando vermelho padrao");
            return defaultColor;
        }
    }

    private Discord.DiscordEmbedField CreateField(string name, string value, bool inline)
    {
        return new Discord.DiscordEmbedField
        {
            Name = name,
            Value = value,
            Inline = inline
        };
    }

    private string FormatSeverity(string severity)
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

    private void LogDebug(string message)
    {
        Log.Debug(message);
    }
}
