using System;
using BepInEx.Bootstrap;

namespace CompetitiveCompany.Compat;

internal static class CompatHelper {
    public static bool CheckFor(string modGuid, Action action) {
        if (!Chainloader.PluginInfos.ContainsKey(modGuid)) return false;

        Log.Info($"{modGuid} detected! Applying compatibility patches...");

        try {
            action();
        } catch (Exception e) {
            Log.Error($"Failed to apply compatibility patches for {modGuid}! {e}");
        }
        return true;
    }
}