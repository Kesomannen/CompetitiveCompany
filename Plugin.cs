using System.Reflection;
using BepInEx;
using CompetitiveCompany.Game;
using CompetitiveCompany.UI;
using HarmonyLib;
using UnityEngine;

namespace CompetitiveCompany;

[
    BepInDependency("ComponentBundler"),
    BepInDependency("evaisa.lethallib"),
    BepInDependency("LethalAPI.Terminal"),
    BepInDependency("com.rune580.LethalCompanyInputUtils"),
]
[BepInPlugin(Guid, Name, Version)]
public class Plugin : BaseUnityPlugin {
    public const string Guid = "com.kesomannen.competitivecompany";
    public const string Name = "CompetitiveCompany";
    public const string Version = "0.1.0";
    
    public new static Config Config { get; private set; } = null!;

    void Awake() {
        Log.Debug("Loading config...");
        Config = new Config(base.Config);
        
        Log.Debug("Patching...");
        var harmony = new Harmony(Guid);
        harmony.PatchAll(typeof(ChatPatches));

        Player.Patch();
        Session.Patch();
        RoundReport.Patch();
        Team.Patch();
        
        Log.Debug("Running InitializeOnLoad methods...");
        RunInitializeOnLoadMethods();
        
        Log.Debug("Initializing terminal commands...");
        TerminalCommands.Initialize();
        
        Log.Debug("Initializing prefabs...");
        NetworkPrefabs.Initialize();
        
        Log.Debug("Loading assets...");
        Assets.Load();
        
        Log.Info("Plugin loaded!");
    }

    static void RunInitializeOnLoadMethods() {
        // needed for UnityNetcodePatcher
        
        var types = Assembly.GetExecutingAssembly().GetTypes();
        foreach (var type in types) {
            var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            
            foreach (var method in methods) {
                var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                if (attributes.Length > 0) {
                    method.Invoke(null, null);
                }
            }
        }
    }
}
