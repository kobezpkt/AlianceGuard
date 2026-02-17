using Exiled.API.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlianceGuard.StringTexts;

public class InitializationTexts
{
    public static void PrintBanner(Version Version, string Author)
    {
        Log.Info("█████╗ ██╗     ██╗ ██████╗███╗   ██╗ ██████╗███████╗");
        Log.Info("██╔══██╗██║     ██║██╔════╝████╗  ██║██╔════╝██╔════╝");
        Log.Info("███████║██║     ██║███████╗██╔██╗ ██║██║     █████╗  ");
        Log.Info("██╔══██║██║     ██║██╔═══██║██║╚██╗██║██║     ██╔══╝  ");
        Log.Info("██║  ██║███████╗██║╚██████╔╝██║ ╚████║╚██████╗███████╗");
        Log.Info("╚═╝  ╚═╝╚══════╝╚═╝ ╚═════╝ ╚═╝  ╚═══╝ ╚═════╝╚══════╝");
        Log.Info("");
        Log.Info($"AlianceGuard v{Version} by {Author}");
        Log.Info("");
    }

    public static void ValidateConfiguration(Config Config)
    {
        if (Config.WebhookEnabled && string.IsNullOrWhiteSpace(Config.WebhookUrl))
        {
            Log.Warn("Webhook esta habilitada mas a URL nao foi configurada!");
        }
    }
}
