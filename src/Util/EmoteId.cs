namespace CompetitiveCompany.Util;

// we can't directly use the Emote enum from BetterEmotes to keep it a soft dependency

/// <summary>
/// Ids of emotes, including the ones from BetterEmots.
/// </summary>
public enum Emote {
    // disable missingxml comment warnings
    #pragma warning disable 1591
    
    Dance = 1,
    Point = 2,
    MiddleFinger = 3,
    Clap = 4,
    Shy = 5,
    Griddy = 6,
    Twerk = 7,
    Salute = 8,
    Prisyadka = 9
    
    #pragma warning restore 1591
}