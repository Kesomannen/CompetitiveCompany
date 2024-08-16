using System;
using Object = UnityEngine.Object;

namespace CompetitiveCompany.Util;

/// <summary>
/// Utils for the vanilla Terminal.
/// </summary>
public static class TerminalUtil {
    static Terminal? _terminal;

    /// <summary>
    /// Gets a cached Terminal instance.
    /// </summary>
    public static Terminal Instance {
        get {
            if (_terminal != null) return _terminal;

            var newTerminal = Object.FindObjectOfType<Terminal>();
            if (newTerminal == null) {
                throw new Exception("Terminal not found in scene");
            }
            
            return _terminal = newTerminal;
        }
    }
}