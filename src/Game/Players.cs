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

    /// <inheritdoc />
    public int Count => _players.Count;
    /// <inheritdoc />
    public Player this[int index] => _players[index];
    
    /// <summary>
    /// Tries to find a player by their username.
    /// <paramref name="comparison"/> defaults to <see cref="StringComparison.OrdinalIgnoreCase"/>.
    /// </summary>
    public bool TryGetByName(string name, StringComparison comparison, [NotNullWhen(true)] out Player? player) {
        foreach (var p in _players) {
            if (!p.Name.Equals(name, comparison)) continue;

            player = p;
            return true;
        }

        player = null;
        return false;
    }
    
    /// <summary>
    /// Tries to find a player by their username, ignoring case.
    /// </summary>
    public bool TryGetByName(string name, [NotNullWhen(true)] out Player? player) {
        return TryGetByName(name, StringComparison.OrdinalIgnoreCase, out player);
    }
    
    /// <summary>
    /// Gets a player by their username. Returns null if not found.
    /// </summary>
    public Player? GetByName(string name, StringComparison comparison = StringComparison.OrdinalIgnoreCase) {
        TryGetByName(name, comparison, out var player);
        return player;
    }

    /// <inheritdoc />
    public IEnumerator<Player> GetEnumerator() => _players.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    
    internal void Register(Player player) => _players.Add(player);
    internal void Deregister(Player player) => _players.Remove(player);
}