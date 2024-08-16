using System.Collections.Generic;

namespace CompetitiveCompany.Game;

/// <summary>
/// Controls PvP combat with a list of predicates.
/// The main method is <see cref="CanDamage"/>, which checks <see cref="Predicates"/> in order,
/// returning the first <see cref="DamagePredicateResult"/> that denies the damage,
/// or <see cref="DamagePredicateResult.Allow"/> if none do.
/// The most used instance of this class is the one stored in <see cref="Session.Combat"/>,
/// which is evaluated each time a player tries to damage another player.
/// </summary>
public class Combat {
    readonly List<DamagePredicate> _predicates;
    
    /// <summary>
    /// The list of predicates that are checked in <see cref="CanDamage"/>.
    /// </summary>
    public IReadOnlyList<DamagePredicate> Predicates => _predicates;
    
    /// <summary>
    /// A predicate that checks if <paramref name="attacker"/> can damage <paramref name="victim"/>.
    /// Either returns <see cref="DamagePredicateResult.Allow"/> to pass the damage through,
    /// or <see cref="DamagePredicateResult.Deny"/> to deny it with a reason.
    /// </summary>
    /// <example>
    /// <code>
    /// // predicate to only allow pvp inside the facility
    /// var predicate = (attacker, victim) => {
    ///     if (!attacker.Controller.isInsideFactory) {
    ///         return DamagePredicateResult.Deny("PvP is only allowed inside the facility!");
    ///     }
    ///
    ///     return DamagePredicateResult.Allow();
    /// }
    /// </code>
    /// </example>
    public delegate DamagePredicateResult DamagePredicate(Player attacker, Player victim);

    /// <summary>
    /// Creates a new instance of <see cref="Combat"/> with no predicates,
    /// making it allow all damage by default.
    /// </summary>
    public Combat() {
        _predicates = [];
    }

    /// <summary>
    /// Creates a new instance of <see cref="Combat"/> with the given predicates.
    /// </summary>
    public Combat(List<DamagePredicate> predicates) {
        _predicates = predicates;
    }
    
    /// <summary>
    /// Adds a new predicate to the list of predicates.
    /// If <paramref name="index"/> is negative, the predicate is added to the end of the list.
    /// </summary>
    public void AddPredicate(DamagePredicate predicate, int index = -1) {
        _predicates.Insert(index < 0 ? _predicates.Count : index, predicate);
    }
    
    /// <summary>
    /// Removes a predicate from the list of predicates.
    /// Returns true if the predicate was found and removed, false otherwise.
    /// </summary>
    public bool RemovePredicate(DamagePredicate predicate) {
        return _predicates.Remove(predicate);
    }
    
    /// <summary>
    /// Checks if <paramref name="attacker"/> can damage <paramref name="victim"/>,
    /// according to <see cref="Predicates"/>. Goes through the list in order and returns
    /// the first <see cref="DamagePredicateResult"/> that denies the damage. If all predicates
    /// pass, it returns <see cref="DamagePredicateResult.Allow"/>.
    /// </summary>
    public DamagePredicateResult CanDamage(Player attacker, Player victim) {
        foreach (var validator in _predicates) {
            var result = validator(attacker, victim);
            if (!result) {
                return result;
            }
        }
        
        return DamagePredicateResult.Allow();
    }
}

/// <summary>
/// The result of a <see cref="Combat.DamagePredicate"/>.
/// </summary>
public readonly struct DamagePredicateResult {
    /// <summary>
    /// Whether the damage attempt was allowed or denied.
    /// </summary>
    public readonly bool Result;
    
    /// <summary>
    /// The reason why the damage was denied, if it was.
    /// </summary>
    public readonly string? Reason;

    DamagePredicateResult(bool result, string? reason = null) {
        Result = result;
        Reason = reason;
    }
    
    /// <summary>
    /// Constructs a new result indicating that the damage is allowed.
    /// </summary>
    public static DamagePredicateResult Allow() => new(true);
    
    /// <summary>
    /// Constructs a new result indicating that the damage is denied, with the given <paramref name="reason"/>.
    /// </summary>
    public static DamagePredicateResult Deny(string reason) => new(false, reason);
    
    public static implicit operator bool(DamagePredicateResult predicateResult) => predicateResult.Result;
}
