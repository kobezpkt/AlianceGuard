using Exiled.API.Interfaces;

namespace AlianceGuard;

public class Config : IConfig
{
    public bool IsEnabled { get; set; } = true;
    public bool Debug { get; set; } = false;

    #region Auto Update
    public bool CheckForUpdates { get; set; } = true;
    public bool AutoUpdate { get; set; } = true;
    #endregion

    #region Webhook
    public bool WebhookEnabled { get; set; } = true;
    public string WebhookUrl { get; set; } = "";
    public string WebhookEmbedColor { get; set; } = "FF0000";
    public string WebhookMentionRole { get; set; } = "";
    #endregion

    #region Sessao (preenchido automaticamente — nao editar)
    public string ApiKey { get; set; } = "";
    public string SessionHmacKey { get; set; } = "";
    public string SessionServerId { get; set; } = "";
    public string SessionExpiresAt { get; set; } = "";
    #endregion
}
