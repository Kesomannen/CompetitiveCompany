﻿using System;
using System.Collections;
using CompetitiveCompany.Game;
using FlowTween;
using UnityEngine;

namespace CompetitiveCompany.UI;

internal class RoundReport : MonoBehaviour {
    [SerializeField] float _autoScrollDuration;
    [SerializeField] RectTransform _autoScrollBar;
    [SerializeField] GameObject _panel;
    [SerializeField] Tab[] _tabs;
    [Space]
    [SerializeField] RoundResultsPage _roundResultsPage;
    [SerializeField] StandingsPage _standingsPage;
    [SerializeField] TeamPerformancePage _teamPerformancePage;

    Tab? _selectedTab;
    Coroutine? _autoScrollCoroutine;

    const float Scale = 0.4f;

    void Start() {
        foreach (var tab in _tabs) {
            tab.Button.OnClicked += OnTabButtonClicked;
        }
        
        if (Application.isEditor) {
            OnRoundEnded(default);
        } else {
            Session.Current.OnRoundEnded += OnRoundEnded;
        }
    }
    
    void OnDestroy() {
        foreach (var tab in _tabs) {
            tab.Button.OnClicked -= OnTabButtonClicked;
        }

        if (!Application.isEditor) {
            Session.Current.OnRoundEnded -= OnRoundEnded;
        }
    }

    void OnRoundEnded(RoundEndedContext ctx) {
        if (!Application.isEditor) {
            GetVanillaElement().gameObject.SetActive(false);
        }
        
        if (ctx.WasLastRound) return;
        
        _roundResultsPage.Populate();
        _standingsPage.Populate();
        _teamPerformancePage.Populate();
        
        SwitchTab(_tabs[0]);
        
        if (Plugin.Config.ShowMouseOnRoundReport.Value) {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Player.Local.Controller.quickMenuManager.isMenuOpen = true;
        } else {
            _autoScrollCoroutine = StartCoroutine(AutoScroll());
        }
        
        _panel.SetActive(true);
    }

    public void Hide() {
        if (Plugin.Config.ShowMouseOnRoundReport.Value) {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Player.Local.Controller.quickMenuManager.isMenuOpen = false;
        }

        _panel.DisableObject();
        
        StopCoroutineIfNotNull(ref _autoScrollCoroutine);
    }
    
    void OnTabButtonClicked(TabButton tabButton) {
        StopCoroutineIfNotNull(ref _autoScrollCoroutine);
        _autoScrollBar.gameObject.SetActive(false);
        
        foreach (var tab in _tabs) {
            if (tab.Button != tabButton) continue;

            SwitchTab(tab);
            break;
        }
    }

    void SwitchTab(Tab tab) {
        if (_selectedTab == tab) return;
        
        if (_selectedTab != null) {
            _selectedTab.Button.Selected = false;
            foreach (var content in _selectedTab.Content) {
                content.SetActive(false);
            }
        }
        
        _selectedTab = tab;
        _selectedTab.Button.Selected = true;
        foreach (var content in _selectedTab.Content) {
            content.SetActive(true);
        }
    }

    IEnumerator AutoScroll() {
        _autoScrollBar.gameObject.SetActive(true);
        
        for (var i = 1; i <= 3; i++) {
            var t = 0f;
            
            _autoScrollBar.anchorMax = Vector2.zero;
            while (t < _autoScrollDuration) {
                var progress = Mathf.SmoothStep(0f, 1f, t / _autoScrollDuration);
                _autoScrollBar.anchorMax = new Vector2(progress, 0f);
                
                yield return null;
                t += Time.deltaTime;
            }

            if (i == 3) {
                Hide();
            } else {
                SwitchTab(_tabs[i]);
            }
        }
    }

    void StopCoroutineIfNotNull(ref Coroutine? coroutine) {
        if (coroutine == null) return;

        StopCoroutine(coroutine);
        coroutine = null;
    }

    internal static Transform GetVanillaElement() {
        return HUDManager.Instance.statsUIElements.allPlayersDeadOverlay.transform.parent.parent;
    }
    
    static RoundReport? _instance;
    
    internal static void Patch() {
        On.HUDManager.OnEnable += (orig, self) => {
            orig(self);

            // Canvas
            //   - EndgameStats
            //     - Text
            //       - AllDead
            var vanillaStats = GetVanillaElement();
            _instance = Instantiate(Assets.RoundReportPrefab, vanillaStats.parent);
            _instance.transform.localScale = Vector3.one * Scale;

            Log.Debug("RoundReport mounted");
        };
        
        On.HUDManager.OnDisable += (orig, self) => {
            if (_instance != null) {
                Destroy(_instance.gameObject);
                _instance = null;
                
                Log.Debug("RoundReport destroyed");
            }
            
            orig(self);
        };
    }
    
    [Serializable]
    class Tab {
        public TabButton Button;
        public GameObject[] Content;
    }
}