//    MIT License
//
//    Copyright (c) 2023 Lethal Company Community
//
//    Permission is hereby granted, free of charge, to any person obtaining a copy
//    of this software and associated documentation files (the "Software"), to deal
//    in the Software without restriction, including without limitation the rights
//    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//    copies of the Software, and to permit persons to whom the Software is
//    furnished to do so, subject to the following conditions:
//
//    The above copyright notice and this permission notice shall be included in all
//    copies or substantial portions of the Software.

using CompetitiveCompany.Game;
using GameNetcodeStuff;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.HighDefinition;

namespace CompetitiveCompany;

internal class SpectatorController : MonoBehaviour {
    #nullable disable // fields are assigned in Awake
    public static SpectatorController Instance { get; private set; }

    const float CamMoveSpeed = 5f;
    
    readonly Light[] _lights = new Light[4];
    
    PlayerControllerB _localPlayer;
    Camera _cam;

    InputAction _upAction;
    InputAction _downAction;
    InputAction _sprintAction;
    InputAction _moveAction;
    
    #nullable restore

    bool _controlsHidden = true;

    public static void Spawn() {
        var obj = new GameObject("SpectatorController", typeof(Camera), typeof(SpectatorController), typeof(AudioListener));
        DontDestroyOnLoad(obj);
    }
    
    void Awake() {
        if (Instance != null) {
            Destroy(this);
            return;
        }

        Instance = this;

        //Set up the lights
        for (var i = 0; i < 4; i++) {
            //Determine the direction it should face

            var dir = i switch {
                0 => new Vector3(50f, 0f, 0f),
                1 => new Vector3(120f, 0f, 0f),
                2 => new Vector3(50f, 90f, 0f),
                3 => new Vector3(50f, -90f, 0f),
                _ => new Vector3()
            };

            //Actually make everything
            var lightObj = new GameObject("GhostLight" + i);
            var light = lightObj.AddComponent<Light>();
            var lightData = lightObj.AddComponent<HDAdditionalLightData>();
            lightObj.transform.eulerAngles = dir;
            light.type = LightType.Directional;
            light.shadows = LightShadows.None;
            light.intensity = 10f;
            lightObj.hideFlags = HideFlags.DontSave;
            lightData.affectsVolumetric = false;
            _lights[i] = light;
        }

        var actions = IngamePlayerSettings.Instance.playerInput.actions;
        _upAction = actions.FindAction("Jump");
        _downAction = actions.FindAction("Crouch");
        _sprintAction = actions.FindAction("Sprint");
        _moveAction = actions.FindAction("Move");

        _cam = GetComponent<Camera>();

        DisableCam();
    }
    
    public void EnableCam() {
        if (gameObject.activeSelf) return;

        gameObject.SetActive(true);

        transform.parent = null;
        var startOfRound = StartOfRound.Instance;
        var oldCam = startOfRound.activeCamera;

        _cam.CopyFrom(oldCam);
        // exclude helmet visor
        var layer = LayerMask.NameToLayer("HelmetVisor");
        _cam.cullingMask &= ~(1 << layer);
        // include invisible enemies
        _cam.cullingMask |= 1 << 23;

        //Move the camera
        transform.position = oldCam.transform.position;
        transform.rotation = oldCam.transform.rotation;

        startOfRound.localPlayerController.ChangeAudioListenerToObject(gameObject);
        startOfRound.localPlayerController.gameplayCamera.enabled = false;
        startOfRound.SwitchCamera(_cam);

        UpdateControlText();

        Log.Debug("Spectator camera enabled!");
    }
    
    public void DisableCam() {
        if (!gameObject.activeSelf) return;
        gameObject.SetActive(false);

        foreach (var light in _lights) {
            light.enabled = false;
        }

        HUDManager.Instance?.ClearControlTips();
    }

    void UpdateControlText() {
        var keybinds = Plugin.Config.Keybinds;
        var vanillaKeybinds = IngamePlayerSettings.Instance.playerInput.actions;

        //If it's hidden, only show how to toggle it
        if (_controlsHidden) {
            HUDManager.Instance.ChangeControlTip(0, $"Show Spectator Controls : [{_(keybinds.ToggleSpectatorControls)}]", clearAllOther: true);
            return;
        }

        //If it's not hidden, show everything
        HUDManager.Instance.ChangeControlTip(0,
            $"""
             Hide Spectator Controls : [{_(keybinds.ToggleSpectatorControls)}]
             Up : [{_(vanillaKeybinds["Jump"])}]
             Down : [{_(vanillaKeybinds["Crouch"])}]
             Toggle Light : [{_(keybinds.SpectatorToggleLight)}]
             Teleport to Player : [0-{StartOfRound.Instance.connectedPlayersAmount - 1}]
             Exit Spectator Mode : [{_(keybinds.ExitSpectator)}]
             """,
            clearAllOther: true
        );
        return;

        string _(InputAction action) {
            // for some reason GetBindingDisplayString() returns the keybind with a pipe character
            return action.GetBindingDisplayString();
        }
    }
    
    void ToggleLight(InputAction.CallbackContext context) {
        //Cancel if this isn't a "performed" action
        if (!context.performed) {
            return;
        }

        //If in the right conditions, switch the light
        if (_localPlayer is { isPlayerDead: true, isTypingChat: false } && !_localPlayer.quickMenuManager.isMenuOpen) {
            foreach (var light in _lights) {
                light.enabled = !light.enabled;
            }
        }
    }

    /**
     * Attempts to teleport to the specified player
     */
    public void TeleportToPlayer(PlayerControllerB player) {
        if (player.isPlayerDead) {
            if (player.deadBody != null && !player.deadBody.deactivated) {
                //Move to the corpse
                transform.position = player.deadBody.transform.position + Vector3.up;
            } else {
                HUDManager.Instance.DisplayTip("Can't Teleport", "Specified player is dead with no body!", true);
            }
        }

        //Player is not connected, can't teleport
        if (!player.isPlayerControlled) {
            HUDManager.Instance.DisplayTip("Can't Teleport", "Specified player is not connected!", true);
            return;
        }

        //Otherwise, move the camera to that player
        transform.position = player.gameplayCamera.transform.position;
        transform.rotation = player.gameplayCamera.transform.rotation;

        //Apply the effects
        _localPlayer.spectatedPlayerScript = player;
        _localPlayer.SetSpectatedPlayerEffects();
    }


    void ToggleControls(InputAction.CallbackContext context) {
        //Don't do things if paused
        if (_localPlayer.isTypingChat || _localPlayer.quickMenuManager.isMenuOpen) {
            return;
        }

        //Swap the visibility and update the text
        _controlsHidden = !_controlsHidden;
        UpdateControlText();
    }
    
    void ExitSpectator(InputAction.CallbackContext context) {
        //Don't do things if paused
        if (_localPlayer.isTypingChat || _localPlayer.quickMenuManager.isMenuOpen) {
            return;
        }

        if (Player.Local.Team != null) {
            HUDManager.Instance.DisplayTip("Can't Exit", "Currently not allowed to leave spectator!", true);
            return;
        }

        Player.Local.StopSpectatingServerRpc();
    }

    void OnEnable() {
        var keybinds = Plugin.Config.Keybinds;
        keybinds.SpectatorToggleLight.performed += ToggleLight;
        keybinds.ToggleSpectatorControls.performed += ToggleControls;
        keybinds.ExitSpectator.performed += ExitSpectator;
    }

    void OnDisable() {
        var keybinds = Plugin.Config.Keybinds;
        keybinds.SpectatorToggleLight.performed -= ToggleLight;
        keybinds.ToggleSpectatorControls.performed -= ToggleControls;
        keybinds.ExitSpectator.performed -= ExitSpectator;
    }

    /**
     * When destroyed, need to manually destroy the ghost light
     */
    void OnDestroy() {
        Log.Debug("SpectatorController destroyed");

        foreach (var light in _lights) {
            if (light != null) {
                Destroy(light.gameObject);
            }
        }
    }

    /**
     * Just before rendering, handle camera input
     */
    void LateUpdate() {
        //Need to wait for the player controller to be registered
        if (_localPlayer == null) {
            _localPlayer = StartOfRound.Instance.localPlayerController;

            if (_localPlayer == null) {
                return;
            }
        }

        //Take raw inputs
        var moveInput = _moveAction.ReadValue<Vector2>();
        var lookInput = _localPlayer.playerActions.Movement.Look.ReadValue<Vector2>() * 0.008f * IngamePlayerSettings.Instance.settings.lookSensitivity;

        if (!IngamePlayerSettings.Instance.settings.invertYAxis) {
            lookInput.y *= -1f;
        }

        var sprint = _sprintAction.ReadValue<float>() > 0.3f;

        transform.Rotate(0, lookInput.x, 0, Space.World);

        //Need to correct the rotation to not allow looking too high or low
        var newX = transform.eulerAngles.x % 360 + lookInput.y;

        if (newX is < 270 and > 90) {
            transform.eulerAngles = 270 - newX < newX - 90 ?
                new Vector3(270, transform.eulerAngles.y, 0) :
                new Vector3(90, transform.eulerAngles.y, 0);
        } else {
            transform.eulerAngles = new Vector3(newX, transform.eulerAngles.y, 0);
        }

        //Move the camera
        var curMoveSpeed = CamMoveSpeed;

        if (sprint) {
            curMoveSpeed *= 5;
        }

        var rightMove = transform.right * moveInput.x * curMoveSpeed * Time.deltaTime;
        var forwardMove = transform.forward * moveInput.y * curMoveSpeed * Time.deltaTime;
        transform.position += rightMove + forwardMove;
        
        var vertMotion = (_upAction.ReadValue<float>() - _downAction.ReadValue<float>()) *
                         curMoveSpeed *
                         Time.deltaTime;
        transform.position += Vector3.up * vertMotion;

        //Lets the player teleport to other players
        var teleIndex = -1;

        for (var i = Key.Digit1; i <= Key.Digit0; i++) {
            if (!Keyboard.current[i].wasPressedThisFrame) continue;

            teleIndex = i - Key.Digit1;
            break;
        }

        if (teleIndex != -1) {
            var playerList = StartOfRound.Instance.allPlayerScripts;

            if (teleIndex >= playerList.Length) {
                HUDManager.Instance.DisplayTip("Cannot Teleport", "Specified player index is invalid!", isWarning: true);
            } else {
                TeleportToPlayer(playerList[teleIndex]);
            }
        }
        
        foreach (var player in StartOfRound.Instance.allPlayerScripts) {
            if (player == _localPlayer || !player.isPlayerControlled) continue;

            player.ShowNameBillboard();
            player.usernameBillboard.LookAt(transform.position);
        }

        // PlayerControllerB shuts this off if no gameplay camera is active, prevent that
        _localPlayer.playerScreen.enabled = true;
    }
}