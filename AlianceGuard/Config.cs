using System.ComponentModel;
using Exiled.API.Interfaces;

namespace AlianceGuard
{
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

        [Description("URL da webhook do Discord")]
        public string WebhookUrl { get; set; } = "";
        [Description("Cor do embed da webhook em hexadecimal (sem #). Vermelho padrao: FF0000")]
        public string WebhookEmbedColor { get; set; } = "FF0000";
        public string WebhookMentionRole { get; set; } = "";

        #endregion
    }
}
