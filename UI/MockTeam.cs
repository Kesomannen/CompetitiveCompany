using System.Collections.Generic;
using CompetitiveCompany.Game;
using UnityEngine;

namespace CompetitiveCompany.UI;

public class MockTeam : ITeam {
    public string Name { get; set; }
    public Color Color { get; set; }
    public int RoundScore { get; set; }
    public int TotalScore { get; set; }

    static readonly MockTeam[] _mockTeams = [
        new MockTeam {
            Name = "Manticoils",
            Color = Color.red,
            RoundScore = 100,
            TotalScore = 1000
        },
        new MockTeam {
            Name = "Hoarding bugs",
            Color = Color.green,
            RoundScore = 200,
            TotalScore = 800
        },
        new MockTeam {
            Name = "Brackens",
            Color = Color.blue,
            RoundScore = 300,
            TotalScore = 1200
        },
        new MockTeam {
            Name = "Locust Bees",
            Color = Color.yellow,
            RoundScore = 400,
            TotalScore = 400
        }
    ];
    
    public static IReadOnlyList<MockTeam> Teams => _mockTeams;
}