using System.Collections.Generic;
using CompetitiveCompany.Game;
using UnityEngine;

namespace CompetitiveCompany.UI;

internal class StandingsPage : MonoBehaviour {
    [SerializeField] TeamResultsPage _teamResultsPage;

    public void Populate() {
        _teamResultsPage.Reset();

        bool isLastRound;
        IReadOnlyList<ITeam> teams;
        
        if (Application.isEditor) {
            isLastRound = true;
            teams = MockTeam.Teams;
        } else {
            isLastRound = Session.Current.RoundNumber == Session.Current.Settings.NumberOfRounds;
            teams = Session.Current.Teams;
        }

        _teamResultsPage.Populate(teams, team => team.TotalScore, isLastRound);
    }
}