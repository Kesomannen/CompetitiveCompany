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
    /// The radius around the ship where players can't damage eachother.
    /// Both the attacker and the victim are required to be outside the radius for damage to be dealt.
    /// Set to 0 to disable.
    /// </summary>
    public ConfigEntry<float> ShipSafeRadius { get; } = file.Bind(
        "General",
        "ShipSafeRadius",
        10f,
        "The radius around the ship where players can't damage eachother.\n" +
        "Both the attacker and the victim are required to be outside the radius for damage to be dealt.\n" +
        "Set to 0 to disable."
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
