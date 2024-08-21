using CompetitiveCompany.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CompetitiveCompany.UI;

internal class TeamRoundResult : MonoBehaviour {
    [SerializeField] TMP_Text _nameText;
    [SerializeField] Image _colorImage;
    [SerializeField] Image _scoreBar;
    [SerializeField] TMP_Text _scoreText;
    [SerializeField] GameObject _winnerIcon;
    
    public void Initialize(ITeam team, int score, int maxScore, bool isWinner) {
        _colorImage.color = team.Color;
        _winnerIcon.SetActive(isWinner);
        
        _nameText.color = team.Color;
        _nameText.text = team.Name;

        _scoreText.enabled = score > 0;
        
        // maximize contrast between text and background
        var colorBrightness = team.Color.r + team.Color.g + team.Color.b;
        _scoreText.color = colorBrightness > 1.5f ? Color.black : Color.white;
        _scoreText.text = "$" + score;
        
        _scoreBar.color = team.Color;
        _scoreBar.rectTransform.anchorMax = new Vector2(score / (float)maxScore, 1f);
    }
}