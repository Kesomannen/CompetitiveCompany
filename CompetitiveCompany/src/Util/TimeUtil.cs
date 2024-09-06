using System;

namespace CompetitiveCompany.Util;

/// <summary>
/// Utility class for time conversions.
/// </summary>
public static class TimeUtil {
    /// <summary>
    /// Converts from normalized time to in-game 24-hour time.
    /// </summary>
    public static TimeSpan NormalizedToGameTime(float normalizedTime) {
        var timeOfDay = TimeOfDay.Instance;
        return TimeSpan.FromHours(normalizedTime * timeOfDay.numberOfHours + 6);
    }
    
    /// <summary>
    /// Get the current in-game 24-hour time.
    /// </summary>
    public static TimeSpan GetCurrentGameTime() {
        return NormalizedToGameTime(TimeOfDay.Instance.normalizedTimeOfDay);
    }
}