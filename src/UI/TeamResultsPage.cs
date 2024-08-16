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

    public void Populate(IEnumerable<ITeam> teams, Func<ITeam, int> getScore, bool showWinner) {
        var orderedTeams = teams.OrderByDescending(getScore).ToArray();
        var maxScore = getScore(orderedTeams[0]);
        
        foreach (var team in orderedTeams) {
            var teamUI = Instantiate(_teamPrefab, _teamContainer);
            var score = getScore(team);
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