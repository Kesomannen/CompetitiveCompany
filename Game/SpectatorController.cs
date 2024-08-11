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

using GameNetcodeStuff;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
//using UnityEngine.Rendering.HighDefinition;

namespace CompetitiveCompany;

public class SpectatorController : MonoBehaviour {
    public static SpectatorController Instance { get; private set; } = null!;
    public PlayerControllerB ClientPlayer { get; private set; } = null!;

    readonly Light[] _lights = new Light[4];
    Camera _cam = null!;
    float _camMoveSpeed = 5f;

    Transform _hintPanelRoot;
    Transform _hintPanelOrigParent;
    Transform _deathUIRoot;
    TextMeshProUGUI _controlsText;
    
    bool _controlsHidden = true;
    float _accelTime = -1;
    float _decelTime = -1;
    bool _altitudeLock;

    /**
     * On awake, make and grab the light
     */
    void Awake() {
        //If the instance already exists, abort!
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
            //var lightData = lightObj.AddComponent<HDAdditionalLightData>();
            lightObj.transform.eulerAngles = dir;
            light.type = LightType.Directional;
            light.shadows = LightShadows.None;
            light.intensity = Plugin.Config.SpectatorLightIntensity.Value;
            lightObj.hideFlags = HideFlags.DontSave;
            //lightData.affectsVolumetric = false;
            _lights[i] = light;
        }

        //Grab the camera and change the mask to include invisible enemies
        _cam = GetComponent<Camera>();
        _cam.cullingMask |= 1 << 23;

        DisableCam();
    }

    /**
     * Enables the spectator camera
     */
    public void EnableCam() {
        if (enabled) return;
        enabled = true;

        //Move the camera
        transform.parent = null;
        var oldCam = StartOfRound.Instance.activeCamera.transform;
        transform.position = oldCam.position;
        transform.rotation = oldCam.rotation;

        //If we don't have them, need to grab certain objects
        if (_hintPanelRoot == null) {
            _hintPanelRoot = HUDManager.Instance.tipsPanelAnimator.transform.parent;
            _hintPanelOrigParent = _hintPanelRoot.parent;
            _deathUIRoot = HUDManager.Instance.SpectateBoxesContainer.transform.parent;

            //Also, make the controls display guy
            var go = Instantiate(HUDManager.Instance.holdButtonToEndGameEarlyText.gameObject, _deathUIRoot);
            _controlsText = go.GetComponent<TextMeshProUGUI>();
            go.name = "PoltergeistControlsText";
        }

        _hintPanelRoot.parent = _deathUIRoot;

        UpdateControlText();
    }

    void UpdateControlText() {
        var keybinds = Plugin.Config.Keybinds;

        //If it's hidden, only show how to toggle it
        if (_controlsHidden) {
            _controlsText.text = $"Show Spectator Controls; [{_(keybinds.ToggleSpectatorControls)}]";
            return;
        }

        //If it's not hidden, show everything
        _controlsText.text =
            $"""
             Hide Spectator Controls: [{_(keybinds.ToggleSpectatorControls)}]
             Increase Speed: [{_(keybinds.SpectatorAccelerate)}]
             Decrease Speed: [{_(keybinds.SpectatorDecelerate)}]
             Up: [{_(keybinds.SpectatorUp)}]
             Down: [{_(keybinds.SpectatorDown)}]
             Lock Altitude: [{_(keybinds.SpectatorLockAltitude)}]
             Toggle Light: [{_(keybinds.SpectatorToggleLight)}]
             """;
        return;

        string _(InputAction action) {
            return action.GetBindingDisplayString();
        }
    }

    /**
     * Disables the spectator camera
     */
    public void DisableCam() {
        if (!enabled) return;

        enabled = false;

        foreach (var light in _lights) {
            light.enabled = false;
        }

        _altitudeLock = false;

        if (_hintPanelRoot != null) {
            _hintPanelRoot.parent = _hintPanelOrigParent;
        }

        _controlsHidden = true;
    }

    /**
     * When the left mouse is clicked, switch the light
     */
    void ToggleLight(InputAction.CallbackContext context) {
        //Cancel if this isn't a "performed" action
        if (!context.performed) {
            return;
        }

        //If in the right conditions, switch the light
        if (ClientPlayer is { isPlayerDead: true, isTypingChat: false } && !ClientPlayer.quickMenuManager.isMenuOpen) {
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
        ClientPlayer.spectatedPlayerScript = player;
        ClientPlayer.SetSpectatedPlayerEffects();
    }

    /**
     * When scrolling up is done, set the camera up to change speed
     */
    void Accelerate(InputAction.CallbackContext context) {
        _accelTime = Time.time + 0.3f;
        _decelTime = -1;
    }

    /**
     * When scrolling down is done, set the camera up to change speed
     */
    void Decelerate(InputAction.CallbackContext context) {
        _decelTime = Time.time + 0.3f;
        _accelTime = -1;
    }

    /**
     * Lock the player's altitude for the standard movement
     */
    void LockAltitude(InputAction.CallbackContext context) {
        if (!context.performed) {
            return;
        }

        if (ClientPlayer.isTypingChat || ClientPlayer.quickMenuManager.isMenuOpen) {
            return;
        }

        _altitudeLock = !_altitudeLock;
    }

    /**
     * Changes the visibility of the controls on the HUD
     */
    void ToggleControls(InputAction.CallbackContext context) {
        //Only do it if performing
        if (!context.performed) {
            return;
        }

        //Don't do things if paused
        if (ClientPlayer.isTypingChat || ClientPlayer.quickMenuManager.isMenuOpen) {
            return;
        }

        //Swap the visibility and update the text
        _controlsHidden = !_controlsHidden;
        UpdateControlText();
    }

    void OnEnable() {
        var keybinds = Plugin.Config.Keybinds;
        keybinds.SpectatorToggleLight.performed += ToggleLight;
        keybinds.SpectatorAccelerate.performed += Accelerate;
        keybinds.SpectatorDecelerate.performed += Decelerate;
        keybinds.SpectatorLockAltitude.performed += LockAltitude;
        keybinds.ToggleSpectatorControls.performed += ToggleControls;
    }

    void OnDisable() {
        var keybinds = Plugin.Config.Keybinds;
        keybinds.SpectatorToggleLight.performed -= ToggleLight;
        keybinds.SpectatorAccelerate.performed -= Accelerate;
        keybinds.SpectatorDecelerate.performed -= Decelerate;
        keybinds.SpectatorLockAltitude.performed -= LockAltitude;
        keybinds.ToggleSpectatorControls.performed -= ToggleControls;
    }

    /**
     * When destroyed, need to manually destroy the ghost light
     */
    void OnDestroy() {
        foreach (var light in _lights) {
            if (light != null) {
                Destroy(light.gameObject);
            }
        }
    }

    void PositionControlText() {
        //Figure out where to actually put the text
        var tf = _controlsText.transform;
        var bounds = HUDManager.Instance.holdButtonToEndGameEarlyVotesText.textBounds;

        //Need to account for the votes text being empty
        if (bounds.m_Extents.y < 0) {
            tf.position = new Vector3(tf.position.x, HUDManager.Instance.holdButtonToEndGameEarlyVotesText.transform.position.y, tf.position.z);
        } else {
            tf.localPosition = new Vector3(tf.localPosition.x,
                (bounds.min + HUDManager.Instance.holdButtonToEndGameEarlyVotesText.transform.localPosition).y - (_controlsText.bounds.extents.y + 22),
                tf.localPosition.z);
        }
    }

    /**
     * Just before rendering, handle camera input
     */
    void LateUpdate() {
        //Need to wait for the player controller to be registered
        if (ClientPlayer == null) {
            ClientPlayer = StartOfRound.Instance.localPlayerController;

            if (ClientPlayer == null) {
                return;
            }
        }

        //Take raw inputs
        var moveInput = IngamePlayerSettings.Instance.playerInput.actions.FindAction("Move").ReadValue<Vector2>();
        var lookInput = ClientPlayer.playerActions.Movement.Look.ReadValue<Vector2>() * 0.008f * IngamePlayerSettings.Instance.settings.lookSensitivity;

        if (!IngamePlayerSettings.Instance.settings.invertYAxis) {
            lookInput.y *= -1f;
        }

        var sprint = IngamePlayerSettings.Instance.playerInput.actions.FindAction("Sprint").ReadValue<float>() > 0.3f;

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
        var curMoveSpeed = _camMoveSpeed;

        if (sprint) {
            curMoveSpeed *= 5;
        }

        var rightMove = transform.right * moveInput.x * curMoveSpeed * Time.deltaTime;
        Vector3 forwardMove;

        //Normally, just move forward
        if (!_altitudeLock) {
            forwardMove = transform.forward * moveInput.y * curMoveSpeed * Time.deltaTime;
        }

        //If their altitude is locked, need special logic
        else {
            //If they're facing straight down, actually take the up vector
            if (transform.forward.y < -0.99) {
                forwardMove = transform.up;
            }

            //If they're facing straight up, actually take the down vector
            else if (transform.forward.y > 0.99) {
                forwardMove = transform.up * -1;
            }

            //Otherwise, take the forward vector
            else {
                forwardMove = transform.forward;
            }

            //Trim the y component to prevent vertical motion
            forwardMove.y = 0;
            forwardMove = forwardMove.normalized;
            forwardMove = forwardMove * moveInput.y * curMoveSpeed * Time.deltaTime;
        }

        transform.position += rightMove + forwardMove;

        //Handle the vertical controls
        var keybinds = Plugin.Config.Keybinds;

        var vertMotion = (keybinds.SpectatorUp.ReadValue<float>() - keybinds.SpectatorDown.ReadValue<float>()) *
                         curMoveSpeed *
                         Time.deltaTime;
        transform.position += Vector3.up * vertMotion;

        //Actually do the speed change
        if (_accelTime > Time.time) {
            _camMoveSpeed += Time.deltaTime * _camMoveSpeed;
            _camMoveSpeed = Mathf.Clamp(_camMoveSpeed, 0, 100);
        } else if (_decelTime > Time.time) {
            _camMoveSpeed -= Time.deltaTime * _camMoveSpeed;
            _camMoveSpeed = Mathf.Clamp(_camMoveSpeed, 0, 100);
        }

        //Lets the player teleport to other players
        var teleIndex = -1;

        for (var i = Key.Digit1; i <= Key.Digit0; i++) {
            if (!Keyboard.current[i].wasPressedThisFrame) continue;

            teleIndex = i - Key.Digit1;
            break;
        }

        if (teleIndex != -1) {
            PlayerControllerB[] playerList = StartOfRound.Instance.allPlayerScripts;

            if (teleIndex >= playerList.Length) {
                HUDManager.Instance.DisplayTip("Cannot Teleport", "Specified player index is invalid!", isWarning: true);
            } else {
                TeleportToPlayer(playerList[teleIndex]);
            }
        }

        PositionControlText();

        foreach (var player in StartOfRound.Instance.allPlayerScripts) {
            if (player == ClientPlayer || !player.isPlayerControlled) continue;

            player.ShowNameBillboard();
            player.usernameBillboard.LookAt(transform.position);
        }
    }
}