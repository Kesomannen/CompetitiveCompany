using System;
using BepInEx.Bootstrap;

namespace CompetitiveCompany.Compat;

internal static class SoftDependencyHelper {
    public static bool CheckFor(string modName, string modGuid, Action action) {
        if (!Chainloader.PluginInfos.ContainsKey(modGuid)) return false;

        Log.Info($"Soft dependency {modName} detected!");

        try {
            action();
        } catch (Exception e) {
            Log.Error($"Failed to apply compatibility patches for {modName}! {e}");
        }
        return true;
    }
}