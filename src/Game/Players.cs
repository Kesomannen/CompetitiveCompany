using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CompetitiveCompany.Game;

/// <summary>
/// A specialized list of <see cref="Player"/> with some utility methods.
/// </summary>
public class Players : IReadOnlyList<Player> {
    readonly List<Player> _players = [];

    public int Count => _players.Count;
    public Player this[int index] => _players[index];

    public bool TryGetByName(string name, StringComparison comparison, [NotNullWhen(true)] out Player? player) {
        foreach (var p in _players) {
            if (!p.Controller.playerUsername.Equals(name, comparison)) continue;

            player = p;
            return true;
        }

        player = null;
        return false;
    }
    
    public bool TryGetByName(string name, [NotNullWhen(true)] out Player? player) {
        return TryGetByName(name, StringComparison.OrdinalIgnoreCase, out player);
    }
    
    public Player? GetByName(string name, StringComparison comparison = StringComparison.OrdinalIgnoreCase) {
        TryGetByName(name, comparison, out var player);
        return player;
    }

    public IEnumerator<Player> GetEnumerator() => _players.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    
    internal void Register(Player player) => _players.Add(player);
    internal void Unregister(Player player) => _players.Remove(player);
}