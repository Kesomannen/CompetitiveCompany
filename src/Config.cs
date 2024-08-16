using BepInEx.Configuration;
using CompetitiveCompany.Game;

namespace CompetitiveCompany;

/// <summary>
/// Configuration for the plugin.
/// The user's config can be read from <c>CompetitiveCompany.Plugin.Config</c>.
/// However, you should usually use <c>Session.Current.Settings</c>, which is a sub-set
/// of <see cref="Config"/> synced from the host to all clients.
/// </summary>
public class Config(ConfigFile file) {
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
            new AcceptableValueRange<int>(1, 50)
        )
    );

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
