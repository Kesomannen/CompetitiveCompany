using System;
using System.Collections.Generic;
using System.Linq;
using CompetitiveCompany.Game;
using UnityEngine;

namespace CompetitiveCompany.UI;

internal class TeamResultsPage : MonoBehaviour {
    [SerializeField] TeamRoundResult _teamPrefab;
    [SerializeField] Transform _teamContainer;
    
    readonly List<TeamRoundResult> _teamUIs = [];

    public void Populate(IEnumerable<ITeam> teams, TeamMetric metric, bool showWinner) {
        var orderedTeams = teams.OrderByDescending(team => team.GetScore(metric)).ToArray();
        var maxScore = orderedTeams[0].GetScore(metric);
        
        foreach (var team in orderedTeams) {
            var teamUI = Instantiate(_teamPrefab, _teamContainer);
            var score = team.GetScore(metric);
            teamUI.Initialize(team, score, maxScore, showWinner && score == maxScore);
            _teamUIs.Add(teamUI);
        }
    }

    public void Reset() {
        foreach (var teamUI in _teamUIs) {
            Destroy(teamUI.gameObject);
        }
        
        _teamUIs.Clear();
    }
}