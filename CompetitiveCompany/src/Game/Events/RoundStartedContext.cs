namespace CompetitiveCompany.Game;

/// <summary>
/// Context for the <see cref="Session.OnRoundStarted"/> event.
/// </summary>
public readonly struct RoundStartedContext {
    /// <summary>
    /// The number of the round that just started.
    /// </summary>
    public readonly int RoundNumber;

    /// <summary>
    /// The current session.
    /// </summary>
    public readonly Session Session;

    internal RoundStartedContext(Session session, int roundNumber) {
        Session = session;
        RoundNumber = roundNumber;
    }
}