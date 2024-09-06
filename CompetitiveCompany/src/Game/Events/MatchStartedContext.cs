namespace CompetitiveCompany.Game;

/// <summary>
/// Context for the <see cref="Session.OnMatchStarted"/> event.
/// </summary>
public readonly struct MatchStartedContext {
    /// <summary>
    /// The current session.
    /// </summary>
    public readonly Session Session;
    
    internal MatchStartedContext(Session session) {
        Session = session;
    }
}