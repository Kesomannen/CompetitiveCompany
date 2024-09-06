using BepInEx.Logging;

namespace CompetitiveCompany;

internal static class Log {
    public static ManualLogSource Source { get; private set; }

    static Log() {
        Source = Logger.CreateLogSource("CompetitiveCompany");
    }
    
    public static void Fatal(string message) => Source.LogFatal(message);
    public static void Error(string message) => Source.LogError(message);
    public static void Warning(string message) => Source.LogWarning(message);
    public static void Message(string message) => Source.LogMessage(message);
    public static void Info(string message) => Source.LogInfo(message);
    public static void Debug(string message) => Source.LogDebug(message);
}