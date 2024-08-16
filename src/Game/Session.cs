﻿using System;
using System.Collections;
using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace CompetitiveCompany.Game;

/// <summary>
/// The main class which manages the teams, players and flow of a match.
/// You can use <c>Session.Current</c> to access the current session.
/// <br/><br/>
/// Session lives as long as <c>StartOfRound</c>, from the user joining a lobby to them leaving it.
/// During a session, there may be any number of matches, each with a number of rounds.
/// </summary>
public class Session : NetworkBehaviour {
    readonly NetworkVariable<NetworkedSettings> _settings = new();
    readonly NetworkVariable<int> _roundNumber = new(-1);

    /// <summary>
    /// Whether a round is active; i.e. a moon is loaded.
    /// </summary>
    public bool IsRoundActive { get; private set; }
    
    /// <summary>
    /// Whether a match is active. This is false before the host pulls the lever for the first time
    /// or after a previous match has ended.
    /// </summary>
    public bool IsMatchActive => RoundNumber >= 0;

    /// <summary>
    /// The ongoing session. In the main menu this will be null, otherwise it's guaranteed to be set.
    /// </summary>
    public static Session Current { get; private set; } = null!;

    /// <summary>
    /// The current round number, starting at 0.
    /// In the pre-game, while the match has not started yet, this is -1.
    /// </summary>
    public int RoundNumber {
        get => _roundNumber.Value;
        private set => _roundNumber.Value = value;
    }

    /// <summary>
    /// A sub-set of <see cref="Config"/> which is synced across the network.
    /// </summary>
    public NetworkedSettings Settings => _settings.Value;

    /// <summary>
    /// List of all teams in the session. <see cref="Teams"/> implements <see cref="IReadOnlyList{Team}"/>,
    /// so you can use it like any other list, except it's immutable.
    /// </summary>
    /// <seealso cref="Team"/>
    public Teams Teams { get; } = new();

    /// <summary>
    /// List of the players in the session. <see cref="Players"/> implements <see cref="IReadOnlyList{Player}"/>,
    /// so you can use it like any other list, except that it's immutable.
    /// </summary>
    public Players Players { get; } = new();

    public Combat Combat { get; private set; } = null!;

    // only server side!!
    internal HashSet<ulong> CollectedItemIds { get; } = [];

    /// <summary>
    /// The minimum number of teams allowed in a session.
    /// </summary>
    public const int MinTeams = 2;

    /// <summary>
    /// The maximum number of teams allowed in a session.
    /// </summary>
    public const int MaxTeams = 6;

    /// <summary>
    /// Invoked right after a round has started, which happens when all
    /// players have loaded in a new moon.
    /// </summary>
    public event Action? OnRoundStarted;

    /// <summary>
    /// Invoked right after a round has ended, which happens when
    /// a moon has been unloaded (and the performance report pops up).
    /// </summary>
    public event Action? OnRoundEnded;

    /// <summary>
    /// Invoked when a match starts, before the first <see cref="OnRoundEnded"/> is called.
    /// This happens when the host pulls the lever after a previous match has ended (or at the start of the game).
    /// </summary>
    public event Action? OnMatchStarted;
    
    /// <summary>
    /// Invoked when a match ends, which happens right after the last round has ended.
    /// </summary>
    public event Action? OnMatchEnded;
    
    /// <summary>
    /// Invoked when a session starts, which happens in Awake as soon as the user joins a game.
    /// </summary>
    public static event Action<Session>? OnSessionStarted;

    /// <summary>
    /// Invoked when a session ends, which happens when the user leaves the lobby.
    /// </summary>
    public static event Action? OnSessionEnded;

    void Awake() {
        Current = this;

        Combat = new Combat([
            IsPlayingPredicate,
            FriendlyFirePredicate,
            ShipSafeRadiusPredicate
        ]);
    }

    IEnumerator Start() {
        // do this after suits are loaded in
        if (Settings.ForceSuits) {
            RefreshSuits();
        }
        
        yield return null;
        
        if (IsServer) {
            // OnClientConnected isn't called for the host
            OnPlayerJoined(StartOfRound.Instance.localPlayerController);
        }
    }

    public override void OnNetworkSpawn() {
        Log.Message("Session started");

        if (IsServer) {
            SyncSettings();
            ListenToConfigChanges();

            CreateTeamServerRpc("Loot bugs", new Color(0.99f, 0.55f, 0.11f));
            CreateTeamServerRpc("Manticoils", new Color(0.20f, 0.57f, 0.16f));
        }

        if (IsClient) {
            _settings.OnValueChanged += OnSettingsChanged;
        }

        OnSessionStarted?.Invoke(this);
    }

    public override void OnNetworkDespawn() {
        if (IsServer) {
            UnlistenToConfigChanges();
        }
        
        if (IsClient) {
            _settings.OnValueChanged -= OnSettingsChanged;
        }
        
        Log.Message("Session stopped");
        OnSessionEnded?.Invoke();
    }

    /// <summary>
    /// Creates a team with the given name and color.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void CreateTeamServerRpc(FixedString128Bytes teamName, Color color) {
        var team = Instantiate(NetworkPrefabs.Team);
        team.GetComponent<NetworkObject>().Spawn();

        team.RawName = teamName;
        team.Color = color;

        Log.Debug($"Created team {teamName}");
    }

    void OnPlayerJoined(PlayerControllerB controller) {
        var player = controller.GetComponent<Player>();
        Log.Debug($"{player} joined the game");

        player.SetTeamFromServer(Teams.GetSmallest());
    }
    
    void StartRound() {
        if (IsRoundActive) return;
        
        RoundNumber++;
        foreach (var team in Teams) {
            team.RoundScore = 0;
        }
        
        StartRoundClientRpc();
    }

    [ClientRpc]
    void StartRoundClientRpc() {
        IsRoundActive = true;

        if (!IsMatchActive) {
            OnMatchStarted?.Invoke();
        }
        
        OnRoundStarted?.Invoke();
        Log.Info($"Round {RoundNumber + 1} started");
    }
    
    void EndRound() {
        if (!IsRoundActive) return;
        EndRoundClientRpc();
    }

    [ClientRpc]
    void EndRoundClientRpc() {
        Log.Info($"Round {RoundNumber} ended");
        IsRoundActive = false;
        OnRoundEnded?.Invoke();
        
        // round starts with 0
        if (RoundNumber >= Settings.NumberOfRounds - 1) {
            EndMatch();
        }
    }

    void EndMatch() {
        Log.Debug("Match ended");
        OnMatchEnded?.Invoke();

        if (!IsServer) return;
        
        foreach (var team in Teams) {
            team.TotalScore = 0;
        }
        
        RoundNumber = -1;
    }

    void OnSettingsChanged(NetworkedSettings prev, NetworkedSettings current) {
        if (prev.ForceSuits != current.ForceSuits) {
            RefreshSuits();
        }
        
        TimeOfDay.Instance.UpdateProfitQuotaCurrentTime();
    }

    public void RefreshSuits() {
        var suits = FindObjectsOfType<UnlockableSuit>(includeInactive: true);
        if (Settings.ForceSuits) {
            Log.Debug("Hiding suit rack and forcing suits on players");
            foreach (var unlockableSuit in suits) {
                unlockableSuit.gameObject.SetActive(false);
            }

            foreach (var player in Players) {
                player.WearTeamSuit();
            }
        } else {
            Log.Debug("Showing suit rack and positioning suits");
            foreach (var suit in suits) {
                suit.gameObject.SetActive(true);
            }
            StartOfRound.Instance.PositionSuitsOnRack();
        }
    }

    void ListenToConfigChanges() {
        var _ = Plugin.Config;
        _.ForceSuits.SettingChanged += OnSyncedEntryChanged;
        _.FriendlyFire.SettingChanged += OnSyncedEntryChanged;
        _.ShipSafeRadius.SettingChanged += OnSyncedEntryChanged;
        _.NumberOfRounds.SettingChanged += OnSyncedEntryChanged;

        _.JoinTeamPerm.SettingChanged += OnSyncedEntryChanged;
        _.CreateAndDeleteTeamPerm.SettingChanged += OnSyncedEntryChanged;
        _.EditTeamPerm.SettingChanged += OnSyncedEntryChanged;
    }

    void UnlistenToConfigChanges() {
        var _ = Plugin.Config;
        _.ForceSuits.SettingChanged -= OnSyncedEntryChanged;
        _.FriendlyFire.SettingChanged -= OnSyncedEntryChanged;
        _.ShipSafeRadius.SettingChanged -= OnSyncedEntryChanged;
        _.NumberOfRounds.SettingChanged -= OnSyncedEntryChanged;
        
        _.JoinTeamPerm.SettingChanged -= OnSyncedEntryChanged;
        _.CreateAndDeleteTeamPerm.SettingChanged -= OnSyncedEntryChanged;
        _.EditTeamPerm.SettingChanged -= OnSyncedEntryChanged;
    }

    void OnSyncedEntryChanged(object? sender, EventArgs e) {
        SyncSettings();
    }

    void SyncSettings() {
        Log.Debug("Syncing settings");
        _settings.Value = new NetworkedSettings(Plugin.Config);
    }

    DamagePredicateResult IsPlayingPredicate(Player attacker, Player victim) {
        return IsRoundActive ? DamagePredicateResult.Allow() : DamagePredicateResult.Deny("Combat is disabled while in orbit");
    }

    DamagePredicateResult FriendlyFirePredicate(Player attacker, Player victim) {
        return Settings.FriendlyFire || attacker.Team != victim.Team
            ? DamagePredicateResult.Allow()
            : DamagePredicateResult.Deny("Friendly fire is disabled");
    }
    
    DamagePredicateResult ShipSafeRadiusPredicate(Player attacker, Player victim) {
        var safeRadius = Settings.ShipSafeRadius;
        if (safeRadius <= 0) return DamagePredicateResult.Allow();

        var shipCenter = StartOfRound.Instance.shipBounds.bounds.center;

        if (
            DistanceBetween(attacker, shipCenter) < safeRadius ||
            DistanceBetween(victim, shipCenter) < safeRadius
        ) {
            return DamagePredicateResult.Deny("Inside ship safe radius");
        }

        return DamagePredicateResult.Allow();
        
        float DistanceBetween(Component player, Vector3 point) {
            return Vector3.Distance(player.transform.position, point);
        }
    }
    
    internal static void Patch() {
        On.StartOfRound.Awake += (orig, self) => {
            orig(self);
            
            if (NetworkManager.Singleton.IsServer) {
                var session = Instantiate(NetworkPrefabs.Session);
                session.GetComponent<NetworkObject>().Spawn();
            }
        };

        On.StartOfRound.OnDestroy += (orig, self) => {
            if (NetworkManager.Singleton.IsServer) {
                Current.NetworkObject.Despawn(destroy: true);
            }
            
            orig(self);
        };

        On.StartOfRound.OnClientConnect += (orig, self, clientId) => {
            orig(self, clientId);

            if (self.IsServer && self.ClientPlayerList.TryGetValue(clientId, out var playerIndex)) {
                Current.OnPlayerJoined(self.allPlayerScripts[playerIndex]);
            }
        };

        On.StartOfRound.StartGame += (orig, self) => {
            if (self.IsServer) {
                Current.StartRound();
            }
            orig(self);
        };

        On.StartOfRound.EndOfGameClientRpc += (orig, self, bodiesInsured, daysPlayersSurvived, connectedPlayersOnServer, scrapCollectedOnServer) => {
            if (self.IsServer) {
                Current.EndRound();
            }
            orig(self, bodiesInsured, daysPlayersSurvived, connectedPlayersOnServer, scrapCollectedOnServer);
        };

        // add pvp check
        On.GameNetcodeStuff.PlayerControllerB.DamagePlayerFromOtherClientServerRpc += (orig, self, amount, direction, playerWhoHit) => {
            var victim = self.GetComponent<Player>();
            var attacker = StartOfRound.Instance.allPlayerScripts[playerWhoHit].GetComponent<Player>();

            var result = Current.Combat.CanDamage(attacker, victim);
            
            #if DEBUG
            var resultStr = result.Result ? "Allowed" : $"Denied: {result.Reason}";
            Log.Debug($"{attacker} tried to damage {victim}: {resultStr}");
            #endif
            
            if (result.Result) {
                orig(self, amount, direction, playerWhoHit);
            } else {
                HUDManager.Instance.DisplayTip("PvP is not allowed!", result.Reason, isWarning: true);
            }
        };
    }
}