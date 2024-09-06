namespace CompetitiveCompany.Game;

/// <summary>
/// Context for the <see cref="Session.OnRoundEnded"/> event.
/// </summary>
public readonly struct RoundEndedContext {
    /// <summary>
    /// Whether this was the last round of the match.
    /// </summary>
    public readonly bool WasLastRound;
    
    /// <summary>
    /// The number of the round that just ended.
    /// </summary>
    public readonly int RoundNumber;
    
    /// <summary>
    /// The current session.
    /// </summary>
    public readonly Session Session;
    
    internal RoundEndedContext(Session session, bool wasLastRound, int roundNumber) {
        Session = session;
        WasLastRound = wasLastRound;
        RoundNumber = roundNumber;
    }
}