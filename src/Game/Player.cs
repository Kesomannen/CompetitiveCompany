using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using BepInEx.Bootstrap;
using CompetitiveCompany.Util;
using GameNetcodeStuff;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using LogLevel = BepInEx.Logging.LogLevel;

namespace CompetitiveCompany.Game;

/// <summary>
/// Manages a player in the game. Guaranteed to be attached to every <c>PlayerControllerB</c> in the scene.
/// You can cheaply access the respective controller from a <see cref="Player"/> with <see cref="Controller"/>.
/// </summary>
public class Player : NetworkBehaviour {
    readonly NetworkVariable<TeamRef> _teamReference = new();

    struct TeamRef : INetworkSerializable {
        public bool HasValue;
        public NetworkBehaviourReference Ref;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
            serializer.SerializeValue(ref HasValue);
            if (HasValue) {
                serializer.SerializeValue(ref Ref);
            }
        }
        
        public bool TryGet([NotNullWhen(true)] out Team? team) {
            if (HasValue && Ref.TryGet(out var behaviour) && behaviour is Team result) {
                team = result;
                return true;
            }

            team = null;
            return false;
        }
    }
    
    readonly NetworkVariable<bool> _isSpectating = new(writePerm: NetworkVariableWritePermission.Owner);
    readonly NetworkVariable<Emote> _endOfMatchEmote = new(writePerm: NetworkVariableWritePermission.Owner);

    /// <summary>
    /// The vanilla controller of the player.
    /// </summary>
    public PlayerControllerB Controller { get; private set; } = null!;
    
    /// <summary>
    /// The player's team, can be null.
    /// Only the player's owner and the server can set this
    /// (through <see cref="SetTeamServerRpc"/> and <see cref="SetTeamFromServer"/>).
    /// </summary>
    public Team? Team { get; private set; }

    /// <summary>
    /// Whether the player is spectating.
    /// </summary>
    public bool IsSpectating => _isSpectating.Value;
    
    /// <summary>
    /// The <see cref="Emote"/> to play at the end of the match, 
    /// picked by the player's owner. Note that still may return an emote that doesn't
    /// exist if BetterEmotes is not installed. <see cref="EndOfMatchEmoteChecked"/> accounts for this.
    /// </summary>
    public Emote EndOfMatchEmote => _endOfMatchEmote.Value;
    
    /// <summary>
    /// Whether the player is controlled by the host/server of the game.
    /// </summary>
    public bool OwnedByHost => OwnerClientId == NetworkManager.ServerClientId;

    public bool IsControlled => Controller.isPlayerControlled;

    Session _session = null!;
    
    /// <summary>
    /// The name of the player, including useful debug information.
    /// </summary>
    /// <example>
    /// <c>Kesomannen [Loot bugs] (Host + Local)</c>
    /// </example>
    public string DebugName {
        get {
            var team = Team?.Name ?? "None";
            var title = OwnedByHost ? "Host" : "Client";
            if (IsOwner) {
                title += " + Local";
            }

            return $"{Controller.playerUsername} [{team}] ({title})";
        }
    }
    
    /// <summary>
    /// The <see cref="Emote"/> to play at the end of the match, picked by the player's owner.
    /// This checks if BetterEmotes is loaded to make sure the emote is valid.
    /// If the player has picked an invalid emote, this falls back to <see cref="Emote.Dance"/>.
    /// </summary>
    public Emote EndOfMatchEmoteChecked {
        get {
            if (!Chainloader.PluginInfos.ContainsKey("BetterEmotes") && !EndOfMatchEmote.IsVanilla()) {
                return Emote.Dance;
            }
            
            return EndOfMatchEmote;
        }
    }

    /// <summary>
    /// The local player.
    /// </summary>
    public static Player Local => StartOfRound.Instance.localPlayerController.GetComponent<Player>();
    
    /// <summary>
    /// Invoked when the player's team changes.
    /// The first argument is the previous team, the second is the new team.
    /// </summary>
    public event Action<Team?, Team?>? OnTeamChanged;

    void Awake() {
        Controller = GetComponent<PlayerControllerB>();
    }

    /// <inheritdoc />
    public override void OnGainedOwnership() {
        base.OnGainedOwnership();
        
        StartCoroutine(Routine());
        return;

        IEnumerator Routine() {
            yield return null;
            var setting = Plugin.Config.EndOfMatchEmote;
            _endOfMatchEmote.Value = setting.Value;
            setting.SettingChanged += OnEndOfMatchEmoteChanged;
        }
    }

    /// <inheritdoc />
    public override void OnLostOwnership() {
        base.OnLostOwnership();
        
        Plugin.Config.EndOfMatchEmote.SettingChanged -= OnEndOfMatchEmoteChanged;
    }

    /// <inheritdoc />
    public override void OnNetworkSpawn() {
        _session = Session.Current;
        _session.Players.Register(this);
        
        _teamReference.OnValueChanged += OnTeamReferenceChanged;

        if (_teamReference.Value.TryGet(out var team)) {
            JoinTeam(team);
        }
    }

    /// <inheritdoc />
    public override void OnNetworkDespawn() {
        _session.Players.Deregister(this);
        
        _teamReference.OnValueChanged -= OnTeamReferenceChanged;

        if (Team != null) {
            LeaveTeam(Team);
        }
    }

    void OnTeamReferenceChanged(TeamRef previous, TeamRef current) {
        if (previous.TryGet(out var prevTeam)) {
            LeaveTeam(prevTeam);
        }
        
        if (current.TryGet(out var newTeam)) {
            JoinTeam(newTeam);
        } 
        
        OnTeamChanged?.Invoke(prevTeam, newTeam);
    }
    
    void JoinTeam(Team team) {
        team.OnColorChanged += OnTeamColorChanged;
        OnTeamColorChanged(team.Color);
        
        team.OnNameChanged += OnTeamNameChanged;
        OnTeamNameChanged(team.Name);
        
        Team = team;
        team.MembersInternal.Add(this);

        var forceSuits = _session.Settings.ForceSuits;
        if (forceSuits || _session.Teams.TryGetFromSuit(Controller.currentSuitID, out _)) {
            // don't do this if the player is wearing a non-team suit
            WearTeamSuit();
        }

        if (IsOwner) {
            // only do this for the local player
            team.OnCreditsChanged += OnTeamCreditsChanged;
            OnTeamCreditsChanged(team.Credits);
            
            if (!forceSuits) {
                _session.RefreshSuits();
            }
        }
        
        PlayerLog($"Joined team {team.Name}", LogLevel.Debug);
    }

    /// <summary>
    /// Sets the player's suit to the team's suit.
    /// Doesn't do anything if the player isn't in a team.
    /// </summary>
    public void WearTeamSuit() {
        if (Team == null) return;
        
        Controller.currentSuitID = Team.SuitId;
        
        Controller.thisPlayerModel.material = Team.SuitMaterial;
        Controller.thisPlayerModelLOD1.material = Team.SuitMaterial;
        Controller.thisPlayerModelLOD2.material = Team.SuitMaterial;
        Controller.thisPlayerModelArms.material = Team.SuitMaterial;
    }

    void LeaveTeam(Team team) {
        team.OnColorChanged -= OnTeamColorChanged;
        
        Team = null;
        team.MembersInternal.Remove(this);

        if (IsOwner) {
            team.OnCreditsChanged -= OnTeamCreditsChanged;
        }
        
        PlayerLog($"Left team {team.Name}", LogLevel.Debug);
    }
    
    void OnTeamColorChanged(Color color) {
        Controller.usernameBillboardText.color = color;
    }
    
    void OnTeamNameChanged(in FixedString128Bytes newValue) {
        Controller.usernameBillboardText.text = $"({newValue}) {Controller.playerUsername}";
    }

    static void OnTeamCreditsChanged(int credits) {
        Log.Debug($"Credits changed: {credits}");
        TerminalUtil.Instance.groupCredits = credits;
        TimeOfDay.Instance.UpdateProfitQuotaCurrentTime();
    }
    
    void SetItemInElevator(GrabbableObject item, bool inShip) {
        if (!inShip || !_session.CollectedItemIds.Add(item.NetworkObjectId)) return;
        
        Team?.AddScore(item.scrapValue);
    }

    /// <summary>
    /// Clears the player's team. Can only be called on the server.
    /// This can also be called by a player's owner with <see cref="ClearTeamServerRpc"/>.
    /// </summary>
    public void ClearTeamFromServer() {
        _teamReference.Value = new TeamRef {
            HasValue = false
        };
    }

    /// <summary>
    /// Sets the player's team. Can only be called on the server.
    /// This can also be called by a player's owner with <see cref="SetTeamServerRpc"/>.
    /// </summary>
    public void SetTeamFromServer(NetworkBehaviourReference reference) {
        if (reference.TryGet(out var behaviour) && behaviour is Team) {
            _teamReference.Value = new TeamRef {
                HasValue = true,
                Ref = reference
            };
        } else {
            PlayerLog("Trying to join an invalid team", LogLevel.Warning);
        }
    }
    
    /// <summary>
    /// Sets the player's team. Can only be called the local player.
    /// This can also be done on the server with <see cref="SetTeamServerRpc"/>.
    /// </summary>
    [ServerRpc(RequireOwnership = true)]
    public void SetTeamServerRpc(NetworkBehaviourReference reference) {
        SetTeamFromServer(reference);
    }
    
    /// <summary>
    /// Clears the player's team. Can only be called the local player.
    /// This can also be done on the server with <see cref="ClearTeamServerRpc"/>.
    /// </summary>
    [ServerRpc(RequireOwnership = true)]
    public void ClearTeamServerRpc() {
        ClearTeamFromServer();
    }

    void PlayerLog(string message, LogLevel level) {
        Log.Source.Log(level, $"{DebugName}: {message}");
    }
    
    void OnEndOfMatchEmoteChanged(object sender, EventArgs e) {
        _endOfMatchEmote.Value = Plugin.Config.EndOfMatchEmote.Value;
    }

    /// <summary>
    /// Starts spectating the game. Can only be called on the local player.
    /// </summary>
    public void StartSpectating() {
        if (!IsOwner) {
            PlayerLog("StartSpectating can only be called on the local player", LogLevel.Error);
            return;
        }
        
        if (IsSpectating) return;
        _isSpectating.Value = true;
        
        ClearTeamServerRpc();
        
        Controller.gameObject.SetActive(false);
        Controller.isPlayerControlled = false;
        SpectatorController.Instance.EnableCam();
    }

    /// <summary>
    /// Stops spectating the game. Can only be called on the local player.
    /// </summary>
    public void StopSpectating() {
        if (!IsOwner) {
            PlayerLog("StopSpectating can only be called on the local player", LogLevel.Error);
            return;
        }
        
        if (!IsSpectating) return;
        _isSpectating.Value = false;
        
        SetTeamServerRpc(_session.Teams.GetSmallest());
        
        Controller.gameObject.SetActive(true);
        Controller.isPlayerControlled = true;
        SpectatorController.Instance.DisableCam();
    }

    /// <inheritdoc />
    public override string ToString() => DebugName;

    internal static void Patch() {
        On.GameNetcodeStuff.PlayerControllerB.SetItemInElevator += (orig, self, inShip, inElevator, item) => {
            orig(self, inShip, inElevator, item);

            if (!self.IsServer) return;
            self.GetComponent<Player>().SetItemInElevator(item, inShip);
        };

        IL.GameNetcodeStuff.PlayerControllerB.SetItemInElevator += il => {
            var c = new ILCursor(il);

            // [2152 11 - 2152 69]
            // RoundManager.Instance.CollectNewScrapForThisRound(gObject);
            c.GotoNext(
                x => x.MatchCall(typeof(RoundManager), "get_Instance"),
                x => x.MatchLdarg(3),
                x => x.MatchCallvirt<RoundManager>(nameof(RoundManager.CollectNewScrapForThisRound))
            );
            
            c.RemoveRange(3);
            
            // this
            c.Emit(OpCodes.Ldarg_0);
            // gObject
            c.Emit(OpCodes.Ldarg_3);
            c.EmitDelegate<Action<PlayerControllerB, GrabbableObject>>((self, item) => {
                var player = self.GetComponent<Player>();
                if (Local.Team == player.Team) {
                    RoundManager.Instance.CollectNewScrapForThisRound(item);
                }
            });
        };
    }
}
