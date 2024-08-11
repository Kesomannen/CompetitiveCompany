using Unity.Netcode;

namespace CompetitiveCompany;

public struct NetworkedSettings : INetworkSerializable {
    public bool FriendlyFire;
    public int NumberOfRounds;
    public float ShipSafeRadius;

    public Permission JoinTeamPerm;
    public Permission CreateAndDeleteTeamPerm;
    public Permission EditTeamPerm;
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        serializer.SerializeValue(ref FriendlyFire);
        serializer.SerializeValue(ref NumberOfRounds);
        serializer.SerializeValue(ref ShipSafeRadius);
        
        serializer.SerializeValue(ref JoinTeamPerm);
        serializer.SerializeValue(ref CreateAndDeleteTeamPerm);
        serializer.SerializeValue(ref EditTeamPerm);
    }

    public static NetworkedSettings FromConfig(Config config) {
        return new NetworkedSettings {
            FriendlyFire = config.FriendlyFire.Value,
            NumberOfRounds = config.NumberOfRounds.Value,
            ShipSafeRadius = config.ShipSafeRadius.Value,
            
            JoinTeamPerm = config.JoinTeamPerm.Value,
            CreateAndDeleteTeamPerm = config.CreateAndDeleteTeamPerm.Value,
            EditTeamPerm = config.EditTeamPerm.Value
        };
    }
}