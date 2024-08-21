using System.Collections;
using System.Collections.Generic;
using CompetitiveCompany.Game;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace CompetitiveCompany.UI;

internal class MatchEndScreen : MonoBehaviour {
    [SerializeField] Light _spotLight;
    [SerializeField] Light _pointLight;
    [SerializeField] GameObject _cameraContainer;
    [SerializeField] Transform _playerPosition;
    [SerializeField] float _showDuration;
    
    Camera _camera;
    Camera _gameplayCamera;
    
    readonly List<GameObject> _playerModels = [];
    readonly Dictionary<Light, bool> _lightStates = new();
    
    static readonly int _emoteNumberID = Animator.StringToHash("emoteNumber");
    
    void Start() {
        Session.Current.OnMatchEnded += OnMatchEnded;
    }

    void Update() {
        if (_cameraContainer.gameObject.activeInHierarchy) return;
        
        if (Keyboard.current.gKey.wasPressedThisFrame) {
            OnMatchEnded(new MatchEndedContext(Player.Local.Team!));
        }
    }

    void OnDestroy() {
        Session.Current.OnMatchEnded -= OnMatchEnded;
    }
    
    void OnMatchEnded(MatchEndedContext ctx) {
        if (!Plugin.Config.ShowEndOfMatchCutscene.Value) return;

        if (_camera == null) {
            CreateCamera();
        }
        
        foreach (var light in FindObjectsOfType<Light>()) {
            _lightStates[light] = light.gameObject.activeSelf;
            light.gameObject.SetActive(false);
        }
        
        _pointLight.color = ctx.WinningTeam.Color;
        
        foreach (var member in ctx.WinningTeam.Members) {
            var model = member.transform.Find("ScavengerModel");
            
            var newModel = Instantiate(model, _playerPosition.position, _playerPosition.rotation);
            
            foreach (Transform child in newModel) {
                child.gameObject.SetActive(true);

                if (child.TryGetComponent(out SkinnedMeshRenderer renderer)) {
                    renderer.enabled = true;
                    renderer.shadowCastingMode = ShadowCastingMode.On;
                }
            }
            
            var rig = newModel.Find("metarig");
            
            var animator = rig.GetComponent<Animator>();
            animator.SetInteger(_emoteNumberID, (int)member.EndOfMatchEmoteChecked);
            animator.SetLayerWeight(animator.GetLayerIndex("UpperBodyEmotes"), 0);
            animator.SetLayerWeight(animator.GetLayerIndex("EmotesNoArms"), 1);
            
            rig.Find("ScavengerModelArmsOnly").gameObject.SetActive(false);
            rig.Find("CameraContainer").gameObject.SetActive(false);
            
            _playerModels.Add(newModel.gameObject);
        }
        
        foreach (var player in Session.Current.Players) {
            player.Controller.gameObject.SetActive(false);
        }

        _cameraContainer.SetActive(true);
        _spotLight.gameObject.SetActive(true);
        _pointLight.gameObject.SetActive(true);
        
        StartOfRound.Instance.SwitchCamera(_camera);
        HUDManager.Instance.HideHUD(true);
        
        StartCoroutine(WaitThenHide());
        return;

        IEnumerator WaitThenHide() {
            yield return new WaitForSeconds(_showDuration);
            Hide();
        }
    }

    void Hide() {
        foreach (var (light, state) in _lightStates) {
            light.gameObject.SetActive(state);
        }
        
        _lightStates.Clear();
        
        foreach (var model in _playerModels) {
            Destroy(model);
        }
        
        _playerModels.Clear();
        
        foreach (var player in Session.Current.Players) {
            player.Controller.gameObject.SetActive(true);
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
        _camera.transform.localPosition = new Vector3(7.5f, 2.1f, -14.3f);
    }

    static MatchEndScreen? _instance;

    public static void Patch() {
        On.StartOfRound.Awake += (orig, self) => {
            orig(self);
            
            if (_instance == null) {
                _instance = Instantiate(Assets.MatchEndScreenPrefab);
            }
        };

        On.StartOfRound.OnDestroy += (orig, self) => {
            if (_instance != null) {
                Destroy(_instance);
                _instance = null;
            }
            
            orig(self);
        };
    }
}