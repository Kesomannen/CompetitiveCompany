using System;
using Object = UnityEngine.Object;

namespace CompetitiveCompany.Util;

public static class TerminalUtil {
    static Terminal? _terminal;

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