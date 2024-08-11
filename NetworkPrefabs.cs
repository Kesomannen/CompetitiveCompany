using CompetitiveCompany.Game;

namespace CompetitiveCompany;

internal static class NetworkPrefabs {
    public static Session Session { get; }
    public static Team Team { get; }
    
    static NetworkPrefabs() {
        Session = LethalLib.Modules.NetworkPrefabs.CreateNetworkPrefab("Session").AddComponent<Session>();
        Team = LethalLib.Modules.NetworkPrefabs.CreateNetworkPrefab("Team").AddComponent<Team>();
        
        Log.Debug("Created NetworkPrefabs");
    }

    public static void Initialize() {
        // do nothing, just trigger the static constructor
    }
}