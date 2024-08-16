using System.Collections.Generic;
using System.Linq;
using CompetitiveCompany.Game;
using TMPro;
using UnityEngine;

namespace CompetitiveCompany.UI;

internal class TeamPerformancePage : MonoBehaviour {
    [SerializeField] TeamPerformanceMember _memberPrefab;
    [SerializeField] Transform _memberContainer;
    [SerializeField] TMP_Text _gradeText;
    [SerializeField] TMP_Text _collectedText;
    
    readonly List<TeamPerformanceMember> _memberUIs = [];

    public void Populate() {
        if (Application.isEditor) return;
        
        foreach (var member in _memberUIs) {
            Destroy(member.gameObject);
        }
        
        _memberUIs.Clear();
        
        var localTeam = Player.Local.Team;
        if (localTeam == null) return;

        foreach (var member in localTeam.Members) {
            var outcome = member.Controller.isPlayerDead ?
                member.Controller.causeOfDeath == CauseOfDeath.Abandoned ? 
                    Outcome.Missing : 
                    Outcome.Died :
                Outcome.Survived;

            var stats = StartOfRound.Instance.gameStats.allPlayerStats[member.Controller.playerClientId];
            
            var memberUI = Instantiate(_memberPrefab, _memberContainer);
            memberUI.Initialize(member.Controller.playerUsername, stats.playerNotes, outcome);
            
            _memberUIs.Add(memberUI);
        }
        
        _gradeText.text = CalculateGrade(localTeam);
        _collectedText.text = $"{localTeam.RoundScore}\n{RoundManager.Instance.totalScrapValueInLevel}";
    }

    static string CalculateGrade(Team team) {
        if (team.Members.All(player => player.Controller.isPlayerDead)) {
            return "F";
        }

        var expected = RoundManager.Instance.totalScrapValueInLevel / Session.Current.Teams.Count;
        var percent = team.RoundScore / expected;

        return percent switch {
            > 2f => "SS",
            > 1.5f => "S+",
            > 1f => "S",
            > 0.75f => "A",
            > 0.5f => "B",
            > 0.33f => "C",
            _ => "D"
        };
    }
}