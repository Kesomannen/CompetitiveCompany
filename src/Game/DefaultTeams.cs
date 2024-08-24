using System;
using System.Collections.Generic;
using System.IO;
using CompetitiveCompany.Util;
using Newtonsoft.Json;
using UnityEngine;

namespace CompetitiveCompany.Game;

/// <summary>
/// Handles the default teams, which can be overridden by a JSON file.
/// </summary>
public static class DefaultTeams {
    static readonly TeamDefinition[] _fallback = [
        new TeamDefinition("Loot bugs", new Color(0.99f, 0.55f, 0.11f)),
        new TeamDefinition("Manticoils", new Color(0.20f, 0.57f, 0.16f))
    ];
    
    /// <summary>
    /// The fallback teams, used when the user-made JSON file is missing or invalid.
    /// </summary>
    public static IReadOnlyList<TeamDefinition> Fallback => _fallback;

    const string FileName = "default_teams.json";
    
    /// <summary>
    /// Gets the default teams, either from a user-made JSON file or <see cref="Fallback"/>.
    /// </summary>
    public static TeamDefinition[] Get() {
        var path = Path.Join(BepInEx.Paths.ConfigPath, FileName);
        Log.Debug($"Looking for {FileName} at {path}.");
        if (!File.Exists(path)) {
            Log.Info($"{FileName} file not found, using fallback.");
            return _fallback;
        }
        
        try {
            var json = File.ReadAllText(path);
            var teams = JsonConvert.DeserializeObject<TeamDefinition[]>(json)!;

            return teams.Length switch {
                < Plugin.MinTeams => throw new Exception("Too few teams defined."),
                > Plugin.MaxTeams => throw new Exception("Too many teams defined."),
                _ => teams
            };
        } catch (Exception e) {
            Log.Warning($"Failed to read {FileName}: {e.Message}, using fallback.");
            return _fallback;
        }
    }
}

/// <summary>
/// A JSON serializable definition of a team.
/// Use <see cref="Session.CreateTeamFromDefinition"/> to create an actual team from this.
/// </summary>
public class TeamDefinition {
    /// <summary>
    /// The name of the team, max 64 characters.
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// The color of the team.
    /// </summary>
    public Color Color { get; }
    
    /// <summary>
    /// The default players on the team, specified by their username.
    /// This only applies when a player joins, after that they're free to switch teams (if permitted by the host).
    /// </summary>
    public string[] Players { get; }

    /// <summary>
    /// Creates a new team definition.
    /// </summary>
    public TeamDefinition(string name, Color color, string[]? players = null) {
        Name = name;
        Color = color;
        Players = players ?? [];
    }

    /// <summary>
    /// Creates a new team definition, where the color is provided as a hex string or one of <see cref="ColorUtil.ColorNames"/>.
    /// This is primarily for JSON deserialization, if you want to create a <see cref="TeamDefinition"/> through code,
    /// use the typed constructor instead.
    /// </summary>
    [JsonConstructor]
    public TeamDefinition(string name, string color, string[]? players = null) {
        Name = name;
        Color = ColorUtil.ParseColor(color) ?? throw new Exception($"Invalid color: {color}");
        Players = players ?? [];
    }
}