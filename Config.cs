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
        "Number of rounds to play before the game ends."
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
    /// Who can use the `join` command. The host can always join other players.
    /// </summary>
    public ConfigEntry<Permission> JoinTeamPerm { get; } = file.Bind(
        "Permissions",
        "JoinTeam",
        Permission.Everyone,
        "Who can use the `join` command. The host can always join other players."
    );

    /// <summary>
    /// Who can create and delete teams.
    /// </summary>
    public ConfigEntry<Permission> CreateAndDeleteTeamPerm { get; } = file.Bind(
        "Permissions",
        "CreateAndDeleteTeam",
        Permission.HostOnly,
        "Who can create and delete teams."
    );

    /// <summary>
    /// Who can edit team names and colors.
    /// </summary>
    public ConfigEntry<Permission> EditTeamPerm { get; } = file.Bind(
        "Permissions",
        "EditTeam",
        Permission.Everyone,
        "Who can edit team names and colors."
    );


    /// <summary>
    /// The intensity of the light that is used when spectating.
    /// Client-sided.
    /// </summary>
    public ConfigEntry<float> SpectatorLightIntensity { get; } = file.Bind(
        "Client",
        "SpectatorLightIntensity",
        0.5f,
        ""
    );
}

public enum Permission: byte {
    Everyone,
    HostOnly
}
