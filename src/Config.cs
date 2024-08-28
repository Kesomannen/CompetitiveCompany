using BepInEx.Configuration;
using CompetitiveCompany.Util;

namespace CompetitiveCompany;

/// <summary>
/// Configuration for the plugin.
/// The user's config can be read from <c>CompetitiveCompany.Plugin.Config</c>.
/// However, you should usually use <c>Session.Current.Settings</c>, which is a sub-set
/// of <see cref="Config"/> synced from the host to all clients.
/// </summary>
public class Config(ConfigFile file) {
    // anytime you add fields here, remember to add items to LethalConfigCompat
    // and NetworkedSettings if it should be synced to clients
    
    /// <inheritdoc cref="Keybinds"/>
    public Keybinds Keybinds { get; } = new();
    
    /// <summary>
    /// Whether to force players to wear suits according to their team's color.
    /// </summary>
    public ConfigEntry<bool> ForceSuits { get; } = file.Bind(
        "General",
        "ForceSuits",
        true,
        "Whether to force players to wear suits according to their team's color."
    );

    /// <summary>
    /// Whether players can damage their teammates.
    /// </summary>
    public ConfigEntry<bool> FriendlyFire { get; } = file.Bind(
        "General",
        "FriendlyFire",
        false,
        "Whether players can damage their teammates."
    );
    
    /// <summary>
    /// Number of rounds to play before the game ends.
    /// </summary>
    public ConfigEntry<int> NumberOfRounds { get; } = file.Bind(
        "General",
        "NumberOfRounds",
        3,
        new ConfigDescription(
            "Number of rounds to play before the game ends.",
            new AcceptableValueRange<int>(1, 25)
        )
    );

    /// <summary>
    /// The radius in meters around the ship's center where players can't damage eachother.
    /// Both the attacker and the victim are required to be outside the radius for damage to be dealt.
    /// For scale, the distance to the edge of the ship's balcony is about 13 meters.
    /// The entrance to Experimentation is approximately 100 meters away.
    /// </summary>
    public ConfigEntry<float> ShipSafeRadius { get; } = file.Bind(
        "General",
        "ShipSafeRadius",
        20f,
        "The radius in meters around the ship's center where players can't damage eachother.\n" +
        "Both the attacker and the victim are required to be outside the radius for damage to be dealt.\n" +
        "For scale, the distance to the edge of the ship's balcony is about 13 meters." +
        "The entrance to Experimentation is approximately 100 meters away."
    );

    /// <summary>
    /// Whether to disable the "vote-to-leave" spectator vote.
    /// </summary>
    public ConfigEntry<bool> DisableAutoPilot { get; } = file.Bind(
        "General",
        "DisableAutoPilot",
        true,
        "Whether to disable the \"vote-to-leave\" spectator vote."
    );

    /// <summary>
    /// The minimum time a player can pull the lever to start leaving the current moon.
    /// Specified in hours on a 24-hour clock (the ship arrives at 8 and leaves at 24).
    /// </summary>
    public ConfigEntry<float> MinLeaveTime { get; } = file.Bind(
        "General",
        "MinimumLeaveTime",
        11f,
        new ConfigDescription(
            "The earliest in-game time a player can pull the lever to starting leaving the current moon." +
            "Specified in hours on a 24-hour clock (the ship arrives at 8 and leaves at 24).",
            new AcceptableValueRange<float>(8f, 24f)
        )
    );

    /// <summary>
    /// How many in-game hours it takes for the ship to leave after the lever is pulled.
    /// </summary>
    public ConfigEntry<float> TimeToLeave { get; } = file.Bind(
        "General",
        "TimeToLeave",
        1f,
        new ConfigDescription(
            "How many in-game hours it takes for the ship to leave after the lever is pulled.",
            new AcceptableValueRange<float>(0f, 16f)
        )
    );

    /// <summary>
    /// Who can use the 'join' command.
    /// </summary>
    public ConfigEntry<Permission> JoinTeamPerm { get; } = file.Bind(
        "Permissions",
        "JoinTeam",
        Permission.Everyone,
        "Who can use the 'join' command."
    );
    
    /// <summary>
    /// Who can use the 'set-team-color' and 'set-team-name' commands.
    /// </summary>
    public ConfigEntry<Permission> EditTeamPerm { get; } = file.Bind(
        "Permissions",
        "EditTeam",
        Permission.Everyone,
        "Who can use the 'set-team-color' and 'set-team-name' commands."
    );

    /// <summary>
    /// Who can use the 'create-team' and 'delete-team' commands.
    /// </summary>
    public ConfigEntry<Permission> CreateAndDeleteTeamPerm { get; } = file.Bind(
        "Permissions",
        "CreateAndDeleteTeam",
        Permission.HostOnly,
        "Who can use the 'create-team' and 'delete-team' commands."
    );
    
    
    /// <summary>
    /// If true, you can use the mouse while the round report is shown.
    /// If false, the mouse will be locked and hidden and the report will close after a set time (like vanilla).
    /// </summary>
    public ConfigEntry<bool> ShowMouseOnRoundReport { get; } = file.Bind(
        "Client",
        "ShowMouseOnRoundReport",
        false,
        "If true, you can use the mouse while the round report is shown."+
        "If false, the mouse will be locked and hidden and the report will close after a set time."
    );
    
    /// <summary>
    /// Whether to show a special cutscene after the last round of the match, or just another round report.
    /// </summary>
    public ConfigEntry<bool> ShowEndOfMatchCutscene { get; } = file.Bind(
        "Client",
        "ShowEndOfMatchCutscene",
        true,
        "Whether to show a special cutscene after the last round of the match or just another round report."
    );
    
    /// <summary>
    /// Only Dance and Point are available unless BetterEmotes by KlutzyBubbles is installed.
    /// </summary>
    public ConfigEntry<Emote> EndOfMatchEmote { get; } = file.Bind(
        "Client",
        "EndOfMatchEmote",
        Emote.Dance,
        "The emote to play at the end of the match. " +
        "Only Dance and Point are available unless BetterEmotes by KlutzyBubbles is installed."
    );
}

/// <summary>
/// Who can use a certain command.
/// </summary>
public enum Permission: byte {
    /// <summary>
    /// Clients and server.
    /// </summary>
    Everyone,
    
    /// <summary>
    /// Only the server.
    /// </summary>
    HostOnly
}
