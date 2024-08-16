﻿using System.IO;
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
    BepInDependency("ComponentBundler"),
    BepInDependency("evaisa.lethallib"),
    BepInDependency("LethalAPI.Terminal"),
    BepInDependency("com.rune580.LethalCompanyInputUtils"),
    BepInDependency("ainavt.lc.lethalconfig", SoftDependency)
]
[BepInPlugin(Guid, Name, Version)]
public class Plugin : BaseUnityPlugin {
    /// <summary>
    /// The GUID of the plugin. This is guaranteed to be unique and will never change.
    /// </summary>
    public const string Guid = "com.kesomannen.competitivecompany";
    
    const string Name = "CompetitiveCompany";
    const string Version = "0.1.0";
    
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
        MiscPatches.Patch();
        Team.Patch();
        
        Log.Debug("Running InitializeOnLoad methods...");
        RunInitializeOnLoadMethods();
        
        Log.Debug("Initializing terminal commands...");
        TerminalCommands.Initialize();
        
        Log.Debug("Initializing prefabs...");
        NetworkPrefabs.Initialize();
        
        Log.Debug("Loading assets...");
        Assets.Load();
        
        Log.Debug("Checking for soft dependencies and compatibility patches...");
        CompatHelper.CheckFor("ainavt.lc.lethalconfig", LethalConfigCompat.Initialize);

        var validator = new NetcodeValidator(Guid);
        validator.BindToPreExistingObjectByBehaviour<Player, PlayerControllerB>();

        /*
        Session.OnSessionStarted += _ => {
            SpectatorController.Spawn();
        };
        
        Session.OnSessionEnded += () => {
            var spectatorController = SpectatorController.Instance;
            if (spectatorController != null) {
                Destroy(spectatorController.gameObject);
            }
        };
        */
        
        Log.Info("Plugin loaded!");
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
