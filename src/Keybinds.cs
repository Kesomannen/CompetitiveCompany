using LethalCompanyInputUtils.Api;
using LethalCompanyInputUtils.BindingPathEnums;
using UnityEngine.InputSystem;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CompetitiveCompany;

/// <summary>
/// Customizable keybinds for the mod, using LethalCompanyInputUtils.
/// </summary>
public class Keybinds : LcInputActions {
    # nullable disable
    
    [InputAction(KeyboardControl.F1, Name = "Toggle spectator controls")]
    public InputAction ToggleSpectatorControls { get; private set; }

    [InputAction(KeyboardControl.F, Name = "Toggle spectator light")]
    public InputAction SpectatorToggleLight { get; private set; }
    
    # nullable restore
}