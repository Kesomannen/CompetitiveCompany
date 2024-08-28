using System.Diagnostics;
using System.IO;
using System.Reflection;
using BepInEx;
using CompetitiveCompany.Compat;
using CompetitiveCompany.Game;
using CompetitiveCompany.UI;
using GameNetcodeStuff;
using HarmonyLib;
using RuntimeNetcodeRPCValidator;
using UnityEngine;
using static BepInEx.BepInDependency.DependencyFlags;

namespace CompetitiveCompany;

/// <summary>
/// Main BepInEx plugin class.
/// </summary>
[
    BepInDependency("evaisa.lethallib"),
    BepInDependency("LethalAPI.Terminal"),
    BepInDependency("com.rune580.LethalCompanyInputUtils"),
    BepInDependency("NicholaScott.BepInEx.RuntimeNetcodeRPCValidator"),
    BepInDependency("ainavt.lc.lethalconfig", SoftDependency),
    BepInDependency("BetterEmotes", SoftDependency)
]
[BepInPlugin(Guid, Name, Version)]
public class Plugin : BaseUnityPlugin {
    /// <summary>
    /// The GUID of the plugin. This is guaranteed to be unique and will never change.
    /// </summary>
    public const string Guid = "com.kesomannen.competitivecompany";
    
    const string Name = "CompetitiveCompany";
    const string Version = "0.1.0";
    
    /// <summary>
    /// Main instance of <see cref="CompetitiveCompany.Config"/>.
    /// </summary>
    public new static Config Config { get; private set; } = null!;
    
    /// <summary>
    /// The minimum number of teams allowed in a session.
    /// </summary>
    public const int MinTeams = 2;

    /// <summary>
    /// The maximum number of teams allowed in a session.
    /// </summary>
    public const int MaxTeams = 6;

    void Awake() {
        var stopwatch = Stopwatch.StartNew();
        
        Log.Debug("Loading config...");
        Config = new Config(base.Config);
        
        Log.Debug("Patching...");
        var harmony = new Harmony(Guid);
        harmony.PatchAll(typeof(ChatPatches));

        Player.Patch();
        MatchEndScreen.Patch();
        RoundReport.Patch();
        MiscPatches.Patch();
        Team.Patch();
        Session.Patch();
        LeverPatches.Patch();
        
        Log.Debug("Running InitializeOnLoad methods...");
        RunInitializeOnLoadMethods();
        
        Log.Debug("Initializing terminal commands...");
        TerminalCommands.Initialize();
        
        Log.Debug("Initializing prefabs...");
        NetworkPrefabs.Initialize();
        
        Log.Debug("Loading assets...");
        Assets.Load();
        
        Log.Debug("Checking for soft dependencies and compatibility patches...");
        SoftDependencyHelper.CheckFor("LethalConfig", "ainavt.lc.lethalconfig", LethalConfigCompat.Initialize);
        // we don't need to do anything in particular for BetterEmotes here,
        // but this will put a log message if it's present
        SoftDependencyHelper.CheckFor("BetterEmotes", "BetterEmotes", () => { }); 
        
        var validator = new NetcodeValidator(Guid);
        validator.BindToPreExistingObjectByBehaviour<Player, PlayerControllerB>();

        Session.OnSessionStarted += _ => {
            SpectatorController.Spawn();
        };
        
        Session.OnSessionEnded += () => {
            var spectatorController = SpectatorController.Instance;
            if (spectatorController != null) {
                Destroy(spectatorController.gameObject);
            }
        };
        
        Log.Info($"Plugin loaded in {stopwatch.ElapsedMilliseconds}ms");
    }

    static void RunInitializeOnLoadMethods() {
        // needed for UnityNetcodePatcher
        
        var types = Assembly.GetExecutingAssembly().GetTypes();
        foreach (var type in types) {
            try {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            
                foreach (var method in methods) {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0) {
                        method.Invoke(null, null);
                    }
                }
            } catch (FileNotFoundException) {
                // thrown if a soft dependency is missing, ignore
            }
        }
    }
}
