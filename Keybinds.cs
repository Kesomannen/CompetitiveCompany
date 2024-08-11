using LethalCompanyInputUtils.Api;
using LethalCompanyInputUtils.BindingPathEnums;
using UnityEngine.InputSystem;

namespace CompetitiveCompany;

public class Keybinds : LcInputActions {
    # nullable disable
    
    [InputAction(KeyboardControl.LeftCtrl, Name = "Toggle spectator controls")]
    public InputAction ToggleSpectatorControls { get; private set; }
    
    [InputAction(KeyboardControl.W, Name = "Spectator accelerate")]
    public InputAction SpectatorAccelerate { get; private set; }
    
    [InputAction(KeyboardControl.S, Name = "Spectator decelerate")]
    public InputAction SpectatorDecelerate { get; private set; }
    
    [InputAction(KeyboardControl.E, Name = "Specator move up")]
    public InputAction SpectatorUp { get; private set; }
    
    [InputAction(KeyboardControl.Q, Name = "Spectator move down")]
    public InputAction SpectatorDown { get; private set; }
    
    [InputAction(KeyboardControl.V, Name = "Lock spectator altitude")]
    public InputAction SpectatorLockAltitude { get; private set; }
    
    [InputAction(KeyboardControl.F, Name = "Toggle spectator light")]
    public InputAction SpectatorToggleLight { get; private set; }
    
    # nullable restore
}