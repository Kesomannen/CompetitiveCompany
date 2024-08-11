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
            if (t.Name.Equals(name, comparison)) continue;

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
         * * -------------------------------------------- *
         * | Team 1    | 100            | 1000            |
         * | Team 2    | 200            | 2000            |
         * | Team 3    | 300            | 3000            |
         * * ============================================ *
         */

        yield return CreateLine('=');
        
        var nameLength = Mathf.Max(4, _list.Max(team => team.RawName.Length));
        var roundScoreLength = Mathf.Max(12, _list.Max(team => team.RoundScore.ToString().Length));
        var totalScoreLength = Mathf.Max(12, _list.Max(team => team.TotalScore.ToString().Length));
        
        var availableSpace = width - 7 - nameLength - roundScoreLength - totalScoreLength;

        if (availableSpace < 0) {
            Log.Warning("Not enough space to print teams");
        } else {
            roundScoreLength += availableSpace / 3;
            availableSpace -= availableSpace / 3;

            totalScoreLength += availableSpace / 2;
            availableSpace -= availableSpace / 2;
        
            nameLength += availableSpace;
        }
        
        yield return CreateRow(
            ("Team", nameLength),
            ("Round Score", roundScoreLength),
            ("Total Score", totalScoreLength)
        );
        
        yield return CreateLine('-');
        
        foreach (var team in _list) {
            yield return CreateRow(
                (color ? team.ColoredName : team.RawName, nameLength),
                (team.RoundScore, roundScoreLength),
                (team.TotalScore, totalScoreLength)
            );
        }
        
        yield return CreateLine('=');

        yield break;

        string CreateRow(params (object, int)[] cols) {
            var row = new StringBuilder();

            foreach (var (content, len) in cols) {
                row.Append($"| {content.ToString().PadRight(len)}");
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