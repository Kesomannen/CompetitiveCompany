using System;
using System.IO;
using System.Reflection;
using BepInEx.Configuration;
using LethalConfig;
using LethalConfig.ConfigItems;
using LethalConfig.ConfigItems.Options;
using Unity.Netcode;
using UnityEngine;

namespace CompetitiveCompany.Compat;

internal static class LethalConfigCompat {
    public static void Initialize() {
        var assembly = Assembly.GetExecutingAssembly();
        
        LethalConfigManager.SkipAutoGen();

        try {
            LethalConfigManager.SetModIcon(ReadIcon(assembly));
        } catch (FileNotFoundException e) {
            Log.Warning($"Mod icon not found at {e.FileName}!");
        }

        var config = Plugin.Config;

        var items = new[] {
            Generate(config.FriendlyFire),
            Generate(config.ForceSuits),
            Generate(config.NumberOfRounds),
            Generate(config.ShipSafeRadius),
            Generate(config.JoinTeamPerm),
            Generate(config.EditTeamPerm),
            Generate(config.CreateAndDeleteTeamPerm),
            Generate(config.ShowEndOfMatchCutscene),
            Generate(config.ShowMouseOnRoundReport),
            Generate(config.EndOfMatchEmote),
        };
        
        foreach (var baseConfigItem in items) {
            LethalConfigManager.AddConfigItem(baseConfigItem);
        }
    }

    static Sprite ReadIcon(Assembly assembly) {
        var iconPath = assembly.Location;
        iconPath = Path.GetDirectoryName(iconPath)!;
        iconPath = Path.Combine(iconPath, "icon.png");
        
        var bytes = File.ReadAllBytes(iconPath);
        var texture = new Texture2D(2, 2);
        texture.LoadImage(bytes);
        
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), 0.5f * Vector2.one);
    }
    
    static BaseConfigItem Generate(ConfigEntry<bool> entry) {
        return new BoolCheckBoxConfigItem(entry,
            new BoolCheckBoxOptions {
                RequiresRestart = false,
                CanModifyCallback = ModifyCallback(entry)
            });
    }
    
    static BaseConfigItem Generate(ConfigEntry<int> entry) {
        var values = entry.Description.AcceptableValues;
        if (values == null) {
            return new IntInputFieldConfigItem(entry,
                new IntInputFieldOptions {
                    RequiresRestart = false,
                    CanModifyCallback = ModifyCallback(entry),
                    Min = int.MinValue,
                    Max = int.MaxValue,
                });
        }

        var range = (AcceptableValueRange<int>) values;
        return new IntSliderConfigItem(entry,
            new IntSliderOptions {
                RequiresRestart = false,
                CanModifyCallback = ModifyCallback(entry),
                Min = range.MinValue,
                Max = range.MaxValue,
            });
    }
    
    static BaseConfigItem Generate(ConfigEntry<float> entry) {
        var values = entry.Description.AcceptableValues;
        if (values == null) {
            return new FloatInputFieldConfigItem(entry,
                new FloatInputFieldOptions {
                    RequiresRestart = false,
                    CanModifyCallback = ModifyCallback(entry),
                    Min = float.MinValue,
                    Max = float.MaxValue,
                });
        }
        
        var range = (AcceptableValueRange<float>) values;
        return new FloatSliderConfigItem(entry,
            new FloatSliderOptions {
                RequiresRestart = false,
                CanModifyCallback = ModifyCallback(entry),
                Min = range.MinValue,
                Max = range.MaxValue,
            });
    }
    
    static BaseConfigItem Generate<T>(ConfigEntry<T> entry) where T : Enum {
        return new EnumDropDownConfigItem<T>(entry,
            new EnumDropDownOptions {
                RequiresRestart = false,
                CanModifyCallback = ModifyCallback(entry)
            });
    }
    
    static BaseOptions.CanModifyDelegate? ModifyCallback(ConfigEntryBase entry) {
        return entry.Definition.Section == "Client" ? null : HostCheck;
    }
    
    static CanModifyResult HostCheck() {
        var network = NetworkManager.Singleton;

        if (network == null || !network.IsListening || network.IsHost) {
            return true;
        }

        return (false, "Only the host can modify this entry.");
    }
}