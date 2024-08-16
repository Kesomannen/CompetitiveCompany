using CompetitiveCompany.Game;
using Unity.Netcode;

namespace CompetitiveCompany;

/// <summary>
/// A sub-set of <see cref="Config"/> that is synced from the server to the clients.
/// You can access the current settings with <see cref="Session.Settings"/>.
/// </summary>
public struct NetworkedSettings(Config config) : INetworkSerializable {
    // Any time you add fields to this struct, remember to add them to the NetworkSerialize method below,
    // in Session.ListenToConfigChanges and Session.UnlistenToConfigChanges.
    
    /// <inheritdoc cref="Config.ForceSuits"/>
    public bool ForceSuits = config.ForceSuits.Value;
    /// <inheritdoc cref="Config.FriendlyFire"/>
    public bool FriendlyFire = config.FriendlyFire.Value;
    /// <inheritdoc cref="Config.NumberOfRounds"/>
    public int NumberOfRounds = config.NumberOfRounds.Value;
    /// <inheritdoc cref="Config.ShipSafeRadius"/>
    public float ShipSafeRadius = config.ShipSafeRadius.Value;

    /// <inheritdoc cref="Config.JoinTeamPerm"/>
    public Permission JoinTeamPerm = config.JoinTeamPerm.Value;
    /// <inheritdoc cref="Config.CreateAndDeleteTeamPerm"/>
    public Permission CreateAndDeleteTeamPerm = config.CreateAndDeleteTeamPerm.Value;
    /// <inheritdoc cref="Config.EditTeamPerm"/>
    public Permission EditTeamPerm = config.EditTeamPerm.Value;

    /// <inheritdoc />
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        serializer.SerializeValue(ref ForceSuits);
        serializer.SerializeValue(ref FriendlyFire);
        serializer.SerializeValue(ref NumberOfRounds);
        serializer.SerializeValue(ref ShipSafeRadius);
        
        serializer.SerializeValue(ref JoinTeamPerm);
        serializer.SerializeValue(ref CreateAndDeleteTeamPerm);
        serializer.SerializeValue(ref EditTeamPerm);
    }
}