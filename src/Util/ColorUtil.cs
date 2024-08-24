using System.Collections.Generic;
using UnityEngine;

namespace CompetitiveCompany.Util;

/// <summary>
/// Extra utilities for working with colors.
/// </summary>
public static class ColorUtil {
    static readonly Dictionary<string, Color> _colorNames = new() {
        { "red", Color.red },
        { "green", Color.green },
        { "blue", Color.blue },
        { "yellow", Color.yellow },
        { "cyan", Color.cyan },
        { "magenta", Color.magenta },
        { "white", Color.white },
        { "black", Color.black }
    };
    
    /// <summary>
    /// Extra color names that can be used in addition to HTML color codes in <see cref="ParseColor"/>.
    /// </summary>
    public static IReadOnlyDictionary<string, Color> ColorNames => _colorNames;

    /// <summary>
    /// Parses a color from a string. The string can either be in HTML hex format (e.g. "#FF00FF") or one of <see cref="ColorNames"/>.
    /// Returns null if the input is not a valid color.
    /// </summary>
    public static Color? ParseColor(string str) {
        if (_colorNames.TryGetValue(str, out var color)) {
            return color;
        }
        
        if (ColorUtility.TryParseHtmlString(str, out color)) {
            return color;
        }
        
        return null;
    }
}