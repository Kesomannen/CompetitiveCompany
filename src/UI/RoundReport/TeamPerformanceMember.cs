using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CompetitiveCompany.UI;

internal class TeamPerformanceMember : MonoBehaviour {
    [SerializeField] TMP_Text _nameText;
    [SerializeField] TMP_Text _notesText;
    [Space]
    [SerializeField] Image _outcomeImage;
    [SerializeField] Sprite _survivedIcon;
    [SerializeField] Sprite _dieIcon;
    [SerializeField] Sprite _missingIcon;
    
    public void Initialize(string name, IEnumerable<string> notes, Outcome outcome) {
        _nameText.text = name;

        var notesBuiler = new StringBuilder("Notes:\n");
        foreach (var note in notes) {
            notesBuiler.Append("* ");
            notesBuiler.AppendLine(note);
        }
        _notesText.text = notesBuiler.ToString();
        
        _outcomeImage.sprite = outcome switch {
            Outcome.Survived => _survivedIcon,
            Outcome.Died => _dieIcon,
            Outcome.Missing => _missingIcon,
            _ => throw new System.ArgumentOutOfRangeException(nameof(outcome), outcome, null)
        };
    }
}

public enum Outcome {
    Survived,
    Died,
    Missing
}