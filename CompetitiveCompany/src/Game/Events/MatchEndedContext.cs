namespace CompetitiveCompany.Game;

/// <summary>
/// Context for the <see cref="Session.OnMatchEnded"/> event.
/// </summary>
public readonly  struct MatchEndedContext {
    /// <summary>
    /// The team that won the match.
    /// </summary>
    public readonly Team WinningTeam;
    
    /// <summary>
    /// The current session.
    /// </summary>
    public readonly Session Session;
    
    internal MatchEndedContext(Session session, Team winningTeam) {
        WinningTeam = winningTeam;
        Session = session;
    }
}