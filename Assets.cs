using System.IO;
using System.Reflection;
using CompetitiveCompany.UI;
using UnityEngine;

namespace CompetitiveCompany;

internal static class Assets {
    public static AssetBundle Bundle { get; }
    public static RoundReport RoundReportPrefab { get; }
    
    static Assets() {
        var path = Assembly.GetExecutingAssembly().Location;
        path = Path.GetDirectoryName(path)!;
        path = Path.Combine(path, "competitive-company");
        
        Bundle = AssetBundle.LoadFromFile(path);
        
        var prefab = Bundle.LoadAsset<GameObject>("Assets/Prefabs/RoundReport.prefab");
        RoundReportPrefab = prefab.GetComponent<RoundReport>();
    }
    
    public static void Load() {
        // do nothing, just trigger the static constructor
    }
}