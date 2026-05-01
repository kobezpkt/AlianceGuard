using Exiled.API.Features;
using System;
using System.Collections.Generic;

namespace AlianceGuard.Services;

public class RoleAssignmentService
{
    private static readonly HashSet<string> ValidColors = new(StringComparer.OrdinalIgnoreCase)
    {
        "pink", "red", "brown", "silver", "light_green", "crimson", "cyan",
        "aqua", "deep_pink", "tomato", "yellow", "magenta", "blue_green",
        "orange", "lime", "green", "emerald", "carmine", "nickel", "mint",
        "army_green", "pumpkin", "default", "white", "snow", "black",
        "blue", "purple"
    };

    public void AssignRole(Player player, string roleIngame, string roleColor, Config config)
    {
        if (player == null || !player.IsConnected || !config.GiveAllinceRolesToStaffUponJoining)
            return;

        if (string.IsNullOrWhiteSpace(roleIngame))   
            return;
        
        try
        {
            string resolvedColor = ResolveColor(roleColor);

            player.RankName = roleIngame;
            player.RankColor = resolvedColor;
        }
        catch (Exception ex)
        {
            Log.Error($"[AlianceGuard] [AssignRole] Excecao ao aplicar tag em {player.Nickname}: {ex.Message}");
            Log.Error($"[AlianceGuard] [AssignRole] StackTrace: {ex.StackTrace}");
        }
    }

    private static string ResolveColor(string colorInput)
    {
        if (string.IsNullOrWhiteSpace(colorInput))      
            return "default";      

        string color = colorInput.Trim().ToLower().Replace(" ", "_");

        if (ValidColors.Contains(color))
            return color;     

        Log.Warn($"[AlianceGuard] [ResolveColor] Cor '{color}' nao reconhecida, usando 'default'. Cores validas: {string.Join(", ", ValidColors)}");
        return "default";
    }
}