namespace CompetitiveCompany.Util;

// We want BetterEmotes to be a soft dependency, so we can't use their Emote enum directly

/// <summary>
/// Ids of emotes, including the ones from BetterEmotes.
/// This is only used for config, otherwise emotes are referred to directly by their ID.
/// </summary>
public enum Emote {
    // disable missing xml comment warnings
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

/// <summary>
/// Utils for working with emotes.
/// </summary>
public static class EmoteUtil {
    /// <summary>
    /// Checks whether the emote ID is a vanilla emote.
    /// </summary>
    public static bool IsVanilla(int emoteID) {
        return emoteID is 1 or 2;
    }
}