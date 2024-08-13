using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CompetitiveCompany.Game;

/// <summary>
/// A specialized list of <see cref="Team"/> with some utility methods.
/// </summary>
public class Teams : IReadOnlyList<Team> {
    readonly List<Team> _list = [];
    
    public int Count => _list.Count;
    
    /// <summary>
    /// Returns the team with the fewest members.
    /// </summary>
    public Team GetSmallest() {
        if (Count == 0) {
            throw new InvalidOperationException("No teams to choose from");
        }
        
        return _list.OrderBy(team => team.Members.Count).First();
    }
    
    /// <summary>
    /// Tries to get a team by its name. Returns true if the team was found, false otherwise.
    /// <c>comparison</c> defaults to <see cref="StringComparison.OrdinalIgnoreCase"/> if omitted.
    /// </summary>
    public bool TryGet(string name, StringComparison comparison, [NotNullWhen(true)] out Team? team) {
        foreach (var t in _list) {
            if (!t.Name.Equals(name, comparison)) continue;

            team = t;
            return true;
        }
        
        team = null;
        return false;
    }
    
    /// <summary>
    /// Tries to get a team by its name. Returns true if the team was found, false otherwise.
    /// By default uses <see cref="StringComparison.OrdinalIgnoreCase"/> to compare strings.
    /// </summary>
    public bool TryGet(string name, [NotNullWhen(true)] out Team? team) {
        return TryGet(name, StringComparison.OrdinalIgnoreCase, out team);
    }
    
    /// <summary>
    /// Gets a team by its name. Returns null if the team was not found.
    /// </summary>
    public Team? Get(string name, StringComparison comparison = StringComparison.OrdinalIgnoreCase) {
        TryGet(name, comparison, out var team);
        return team;
    }

    public IEnumerable<string> PrettyPrint(int width, bool color) {
        /*
         * * ============================================ *
         * | Team      | Round Score    | Total Score     |
         * * --------- * -------------- * --------------- *
         * | Team 1    | 100            | 1000            |
         * | Team 2    | 200            | 2000            |
         * | Team 3    | 300            | 3000            |
         * * ========= * ============== * =============== *
         */

        var teamNames = _list.Select(team => {
            var length = team.RawName.Length;
            var name = color ? team.ColoredName : team.Name;

            if (Player.Local.Team == team) {
                name = "> " + name;
                length += 2;
            }

            return (Name: name, DisplayedLength: length);
        }).ToArray();
        
        var nameColWidth = Mathf.Max(4, teamNames.Max(name => name.DisplayedLength));
        var roundScoreColWidth = Mathf.Max(12, _list.Max(team => team.RoundScore.ToString().Length));
        var totalScoreColWidth = Mathf.Max(12, _list.Max(team => team.TotalScore.ToString().Length));
        
        var availableSpace = width - 7 - nameColWidth - roundScoreColWidth - totalScoreColWidth;

        if (availableSpace < 0) {
            Log.Warning("Not enough space to print teams");
        } else {
            roundScoreColWidth += availableSpace / 3;
            availableSpace -= availableSpace / 3;

            totalScoreColWidth += availableSpace / 2;
            availableSpace -= availableSpace / 2;
        
            nameColWidth += availableSpace;
        }
        
        yield return CreateLine('=');
        
        yield return CreateRow(
            ("Team", nameColWidth, null),
            ("Round Score", roundScoreColWidth, null),
            ("Total Score", totalScoreColWidth, null)
        );
        
        yield return CreateLine('-');
        
        for (var i = 0; i < _list.Count; i++) {
            var team = _list[i];

            yield return CreateRow(
                (teamNames[i].Name, nameColWidth, teamNames[i].DisplayedLength),
                (team.RoundScore, roundScoreColWidth, null),
                (team.TotalScore, totalScoreColWidth, null)
            );
        }

        yield return CreateLine('=');

        yield break;

        string CreateRow(params (object, int, int?)[] cols) {
            var row = new StringBuilder();

            foreach (var (content, targetLength, displayedLength) in cols) {
                var str = content.ToString();
                var diff = targetLength - (displayedLength ?? str.Length);
                
                row.Append("| ");
                row.Append(str);
                row.Append(' ', diff);
            }
            
            row.Append("|");
            return row.ToString();
        }
        
        string CreateLine(char c) {
            return $"* {new string(c, width - 4)} *";
        }
    }
    
    internal void Register(Team team) => _list.Add(team);
    internal void Unregister(Team team) => _list.Remove(team);
    
    public Team this[int index] => _list[index];
    
    public IEnumerator<Team> GetEnumerator() => _list.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}