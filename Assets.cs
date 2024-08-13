using System.IO;
using System.Reflection;
using CompetitiveCompany.UI;
using UnityEngine;

namespace CompetitiveCompany;

internal static class Assets {
    public static AssetBundle Bundle { get; }
    public static RoundReport RoundReportPrefab { get; }
    public static Material TeamSuitMaterial { get; }
    
    static Assets() {
        var path = Assembly.GetExecutingAssembly().Location;
        path = Path.GetDirectoryName(path)!;
        path = Path.Combine(path, "competitive-company");
        
        Bundle = AssetBundle.LoadFromFile(path);
        
        var prefab = Bundle.LoadAsset<GameObject>("Assets/CC2/Prefabs/RoundReport.prefab");
        RoundReportPrefab = prefab.GetComponent<RoundReport>();
        
        TeamSuitMaterial = Bundle.LoadAsset<Material>("Assets/CC2/Materials/TeamSuit.mat");
    }
    
    public static void Load() {
        // do nothing, just trigger the static constructor
    }
}