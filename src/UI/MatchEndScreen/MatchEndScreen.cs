using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CompetitiveCompany.Game;
using CompetitiveCompany.Util;
using FlowTween;
using FlowTween.Components;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace CompetitiveCompany.UI;

internal class MatchEndScreen : MonoBehaviour {
    [SerializeField] Light _spotLight;
    [SerializeField] Light _pointLight;
    [SerializeField] GameObject _cameraContainer;
    [SerializeField] Transform _playerPosition;
    [SerializeField] float _playerRadius = 3;
    [SerializeField] float _playerSpacing = Mathf.PI / 8f;
    [SerializeField] float _showDuration;
    [SerializeField] float _showDelay;
    [SerializeField] GameObject _ui;
    [SerializeField] TMP_Text _title;
    [SerializeField] Image _titleBackground;

    Camera _camera;
    Camera _gameplayCamera;

    readonly List<(GameObject, Player)> _playerModels = [];
    readonly Dictionary<Light, bool> _lightStates = new();

    static readonly int _emoteNumberID = Animator.StringToHash("emoteNumber");

    IEnumerator Start() {
        Session.Current.OnMatchEnded += OnMatchEnded;

        yield return null; // wait for stuff to be initialized

        _ui.transform.SetParent(RoundReport.GetVanillaElement().parent);
    }

    void OnDestroy() {
        Session.Current.OnMatchEnded -= OnMatchEnded;
    }

    void OnMatchEnded(MatchEndedContext ctx) {
        if (!Plugin.Config.ShowEndOfMatchCutscene.Value) return;

        this.DelayedCall(_showDelay,
            () => {
                if (_camera == null) {
                    CreateCamera();
                }

                foreach (var light in FindObjectsOfType<Light>()) {
                    _lightStates[light] = light.gameObject.activeSelf;
                    light.gameObject.SetActive(false);
                }

                _pointLight.color = ctx.WinningTeam.Color;
                _title.text = $"{ctx.WinningTeam.RawName} win!";

                var i = 0;
                var members = ctx.WinningTeam.Members;

                foreach (var member in members) {
                    var model = member.transform.Find("ScavengerModel");

                    var (position, rotation) = CalculatePlayerPosition(i, members.Count);
                    var newModel = Instantiate(model, position, rotation);

                    // enable all the renderers
                    foreach (Transform child in newModel) {
                        child.gameObject.SetActive(true);
                        if (!child.TryGetComponent(out SkinnedMeshRenderer renderer)) continue;

                        renderer.enabled = true;
                        renderer.shadowCastingMode = ShadowCastingMode.On;
                    }

                    // make em dance
                    var rig = newModel.Find("metarig");

                    var animator = rig.GetComponent<Animator>();
                    var emote = member.EndOfMatchEmoteChecked;

                    Log.Debug($"Setting emote ID for {member.DebugName} to {emote}");
                    animator.SetInteger(_emoteNumberID, emote);
                    animator.SetLayerWeight(animator.GetLayerIndex("UpperBodyEmotes"), 0);
                    animator.SetLayerWeight(animator.GetLayerIndex("EmotesNoArms"), 1);

                    // disable POV arm model
                    rig.Find("ScavengerModelArmsOnly").gameObject.SetActive(false);
                    // disable cameras
                    rig.Find("CameraContainer").gameObject.SetActive(false);

                    _playerModels.Add((newModel.gameObject, member));
                    i++;
                }

                foreach (var player in Session.Current.Players) {
                    if (player.IsControlled) {
                        player.Controller.gameObject.SetActive(false);
                    }
                }

                _cameraContainer.SetActive(true);
                _spotLight.gameObject.SetActive(true);
                _pointLight.gameObject.SetActive(true);

                _camera!.fieldOfView = 65;
                StartOfRound.Instance.SwitchCamera(_camera);
                HUDManager.Instance.HideHUD(true);

                StartCoroutine(WaitThenHide());

            });
        return;

        IEnumerator WaitThenHide() {
            yield return _ui.SetObjectEnabledRoutine(true);
            yield return new WaitForSeconds(_showDuration);
            yield return _ui.SetObjectEnabledRoutine(false);

            Hide();
        }
    }

    void Hide() {
        foreach (var (light, state) in _lightStates) {
            if (light == null) {
                continue;
            }
            light.gameObject.SetActive(state);
        }

        _lightStates.Clear();

        foreach (var (model, player) in _playerModels) {
            Destroy(model);
            player.Controller.gameObject.SetActive(true);
        }

        _playerModels.Clear();

        foreach (var player in Session.Current.Players) {
            if (player.IsControlled) {
                player.Controller.gameObject.SetActive(true);
            }
        }

        _cameraContainer.SetActive(false);
        _spotLight.gameObject.SetActive(false);
        _pointLight.gameObject.SetActive(false);

        StartOfRound.Instance.SwitchCamera(_gameplayCamera);
        HUDManager.Instance.HideHUD(false);
    }

    void CreateCamera() {
        _gameplayCamera = Player.Local.Controller.gameplayCamera;
        _camera = _cameraContainer.AddComponent<Camera>();
        _camera.CopyFrom(_gameplayCamera);

        var layer = LayerMask.NameToLayer("HelmetVisor");
        // remove layer from the culling mask
        _camera.cullingMask &= ~(1 << layer);

        _camera.transform.rotation = Quaternion.Euler(0, 270, 0);
        _camera.transform.localPosition = new Vector3(7.5f, 1.9f, -14.3f);
    }

    (Vector3, Quaternion) CalculatePlayerPosition(int index, int totalPlayers) {
        // trust me on this one

        var center = _playerPosition.position + new Vector3(_playerRadius, 0, 0);
        var centeredIndex = index - (totalPlayers - 1) / 2f;
        var angle = Mathf.PI - centeredIndex * _playerSpacing;

        var x = center.x + _playerRadius * Mathf.Cos(angle);
        var z = center.z + _playerRadius * Mathf.Sin(angle);

        var position = new Vector3(x, _playerPosition.position.y, z);
        var rotation = Quaternion.LookRotation(center - position);

        return (position, rotation);
    }

    static MatchEndScreen? _instance;

    public static void Patch() {
        On.HUDManager.OnEnable += (orig, self) => {
            orig(self);

            _instance = Instantiate(Assets.MatchEndScreenPrefab);
        };

        On.HUDManager.OnDisable += (orig, self) => {
            if (_instance != null) {
                Destroy(_instance);
                _instance = null;
            }

            orig(self);
        };
    }
}