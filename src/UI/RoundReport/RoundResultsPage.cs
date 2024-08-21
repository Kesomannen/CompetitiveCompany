using System.Collections.Generic;
using CompetitiveCompany.Game;
using UnityEngine;

namespace CompetitiveCompany.UI;

internal class RoundResultsPage : MonoBehaviour {
    [SerializeField] TeamResultsPage _teamResultsPage;

    public void Populate() {
        _teamResultsPage.Reset();
        
        IReadOnlyList<ITeam> teams = Application.isEditor ? MockTeam.Teams : Session.Current.Teams;
        _teamResultsPage.Populate(teams, TeamMetric.RoundScore, true);
    }
}