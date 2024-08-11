using Unity.Netcode;

namespace CompetitiveCompany;

public struct NetworkedSettings(Config config) : INetworkSerializable {
    public bool FriendlyFire = config.FriendlyFire.Value;
    public int NumberOfRounds = config.NumberOfRounds.Value;
    public float ShipSafeRadius = config.ShipSafeRadius.Value;

    public Permission JoinTeamPerm = config.JoinTeamPerm.Value;
    public Permission CreateAndDeleteTeamPerm = config.CreateAndDeleteTeamPerm.Value;
    public Permission EditTeamPerm = config.EditTeamPerm.Value;
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        serializer.SerializeValue(ref FriendlyFire);
        serializer.SerializeValue(ref NumberOfRounds);
        serializer.SerializeValue(ref ShipSafeRadius);
        
        serializer.SerializeValue(ref JoinTeamPerm);
        serializer.SerializeValue(ref CreateAndDeleteTeamPerm);
        serializer.SerializeValue(ref EditTeamPerm);
    }

}