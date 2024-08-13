﻿using System;
using System.Collections.Generic;
using System.Linq;
using CompetitiveCompany.Util;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CompetitiveCompany.Game;

public interface ITeam {
    string Name { get; }
    Color Color { get; }
    int RoundScore { get; }
    int TotalScore { get; }
}

/// <summary>
/// Manages a team in the game. You can get hold of a team by <c>Session.Current.Teams</c> or <c>Player.Team</c>. 
/// </summary>
public class Team : NetworkBehaviour, ITeam {
    readonly NetworkVariable<FixedString128Bytes> _name = new();
    readonly NetworkVariable<Color> _color = new();
    
    readonly NetworkVariable<int> _roundScore = new();
    readonly NetworkVariable<int> _totalScore = new();
    readonly NetworkVariable<int> _credits = new();
    
    internal readonly List<Player> MembersInternal = [];

    /// <summary>
    /// The name of the team. To set this, use <see cref="RawName"/>.
    /// </summary>
    public string Name => _name.Value.ToString();

    /// <summary>
    /// The raw name of the team. Can only be set on the server.
    /// </summary>
    public FixedString128Bytes RawName {
        get => _name.Value;
        set => _name.Value = value;
    }

    /// <summary>
    /// The color of the team. Can only be set on the server.
    /// </summary>
    public Color Color {
        get => _color.Value;
        set => _color.Value = value;
    }
    
    
    /// <summary>
    /// The scrap value collected by the team in the current round.
    /// Can only be set on the server.
    /// </summary>
    public int RoundScore {
        get => _roundScore.Value;
        set => _roundScore.Value = value;
    }
    
    /// <summary>
    /// The total scrap value collected by the team in the current match.
    /// Can only be set on the server.
    /// </summary>
    public int TotalScore {
        get => _totalScore.Value;
        set => _totalScore.Value = value;
    }
    
    /// <summary>
    /// The amount of credits the team has. Can only be set on the server.
    /// </summary>
    public int Credits {
        get => _credits.Value;
        set {
            Log.Debug($"{RawName}: Setting credits to {value}");
            
            _credits.Value = value;
        } 
    }
    
    /// <summary>
    /// The unlockables ID of the team's suit.
    /// </summary>
    public int SuitId { get; private set; }
    
    /// <summary>
    /// The unlockable item of the team's suit.
    /// </summary>
    public UnlockableItem Suit { get; private set; } = null!;
    
    /// <summary>
    /// The material of the team's suit. Automatically updates when the color changes.
    /// </summary>
    public Material SuitMaterial { get; private set; } = null!;
    
    /// <summary>
    /// The members of the team. To add or remove players from teams, see <see cref="Player.SetTeamServerRpc"/>.
    /// </summary>
    public IReadOnlyCollection<Player> Members => MembersInternal;
    
    /// <summary>
    /// Returns the name of the team that, when shown by a TextMeshPro component, will be colored
    /// according to the team's color.
    /// </summary>
    public string ColoredName => $"<color=#{ColorUtility.ToHtmlStringRGB(Color)}>{RawName}</color>";

    Session _session = null!;
    
    static readonly int _normalMapID = Shader.PropertyToID("_NormalMap");

    public event Action<Color>? OnColorChanged;
    public event Action<string>? OnNameChanged; 
    public event Action<int>? OnCreditsChanged; 
    
    void Start() {
        Suit = SuitHelper.CreateSuit("CompetitiveCompanySuit", StartOfRound.Instance);

        SuitMaterial = Instantiate(Assets.TeamSuitMaterial);
        SuitMaterial.mainTexture = Suit.suitMaterial.mainTexture;
        SuitMaterial.SetTexture(_normalMapID, Suit.suitMaterial.GetTexture(_normalMapID));
        Suit.suitMaterial = SuitMaterial;
        OnColorChangedHandler(default, Color);
        
        var unlockables = StartOfRound.Instance.unlockablesList.unlockables;
        SuitId = unlockables.Count;
        unlockables.Add(Suit);
    }
    
    /// <summary>
    /// Adds the given score to the team's credits, round and total score.
    /// Can only be called on the server.
    /// </summary>
    public void AddScore(int score) {
        RoundScore += score;
        TotalScore += score;
        Credits += score;
    }

    public override void OnNetworkSpawn() {
        _session = Session.Current;
        _session.Teams.Register(this);
        
        _color.OnValueChanged += OnColorChangedHandler;
        _name.OnValueChanged += OnNameChangedHandler;
        _credits.OnValueChanged += OnCreditsChangedHandler;
    }
    
    public override void OnNetworkDespawn() {
        _session.Teams.Unregister(this);
        
        _color.OnValueChanged -= OnColorChangedHandler;
        _name.OnValueChanged -= OnNameChangedHandler;
        _credits.OnValueChanged -= OnCreditsChangedHandler;
    }
    
    void OnColorChangedHandler(Color previous, Color current) {
        if (SuitMaterial != null) {
            SuitMaterial.color = current;
        }
        
        OnColorChanged?.Invoke(current);
    }
    
    void OnNameChangedHandler(FixedString128Bytes _, FixedString128Bytes current) {
        OnNameChanged?.Invoke(current.ToString());
    }
    
    void OnCreditsChangedHandler(int previous, int current) {
        OnCreditsChanged?.Invoke(current);
    }

    /// <summary>
    /// Deletes the team. Fails if the team has members.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void DeleteServerRpc() {
        if (Members.Any()) {
            Log.Warning("Cannot delete a team with members");
            return;
        }
        
        NetworkObject.Despawn(destroy: true);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void SetColorServerRpc(Color color) {
        Color = color;
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void SetNameServerRpc(FixedString128Bytes newName) {
        RawName = newName;
    }

    [ServerRpc(RequireOwnership = false)]
    void BuyItemsServerRpc(int[] boughtItems, int newCredits, int numItemsInShip) {
        #if DEBUG
        Log.Debug($"{RawName}: Syncing bought items to clients. New credits: {newCredits}, items in ship: {numItemsInShip}");
        #endif
        
        Credits = newCredits;
        TerminalUtil.Instance.orderedItemsFromTerminal.AddRange(boughtItems);
        SyncItemsInShipClientRpc(numItemsInShip);
    }
    
    [ClientRpc]
    void SyncItemsInShipClientRpc(int itemsInShip) {
        #if DEBUG
        Log.Debug($"Received items in ship from server: {itemsInShip}");
        #endif
        
        TerminalUtil.Instance.numberOfItemsInDropship = itemsInShip;
    }

    [ServerRpc(RequireOwnership = false)]
    void BuyShipUnlockableServerRpc(int id, int newCredits) {
        Credits = newCredits;
        StartOfRound.Instance.UnlockShipObject(id);
        StartOfRound.Instance.BuyShipUnlockableClientRpc(newCredits, id);
    }
    
    [ServerRpc(RequireOwnership = false)]
    void ChangeLevelServerRpc(int levelId, int newCredits) {
        Credits = newCredits;
        StartOfRound.Instance.travellingToNewLevel = true;
        StartOfRound.Instance.ChangeLevelClientRpc(levelId, 0);
    }
    
    [ServerRpc(RequireOwnership = false)]
    void BuyVehicleServerRpc(int vehicleID, int newCredits, bool useWarranty) {
        Credits = newCredits;
        
        var terminal = TerminalUtil.Instance;
        
        terminal.hasWarrantyTicket = !useWarranty;
        terminal.vehicleInDropship = true;
        terminal.orderedVehicleFromTerminal = vehicleID;
        terminal.BuyVehicleClientRpc(0, terminal.hasWarrantyTicket);
    }

    internal static void Patch() {
        // These patches are to make each team have their own credits.
        // This is done by preventing the game from setting Terminal.groupCredits
        // and replacing ServerRpcs on StartOfRound and Terminal with the team-specific RPCs above
        
        IL.Terminal.SyncBoughtItemsWithServer += il => {
            var c = new ILCursor(il);

            c.GotoNext(
                x => x.MatchCall<Terminal>(nameof(Terminal.BuyItemsServerRpc))
            );

            c.Remove();

            c.EmitDelegate<Action<Terminal, int[], int, int>>((_, boughtItems, newGroupCredits, numItemsInShip) => {
                var team = Player.Local.Team;
                if (team == null) return;
                
                team.BuyItemsServerRpc(boughtItems, newGroupCredits, numItemsInShip);
            });
        };
        
        // on the server, SyncGroupCreditsClientRpc is called directly from LoadNewAffordableNode
        On.Terminal.SyncGroupCreditsClientRpc += (_, _, newGroupCredits, itemsInShip) => {
            var team = Player.Local.Team;
            if (team == null) return;
            
            team.Credits = newGroupCredits;
            team.SyncItemsInShipClientRpc(itemsInShip);
            
            // don't call orig here to prevent the RPC from being sent to all clients
        };

        IL.Terminal.LoadNewNodeIfAffordable += il => {
            var c = new ILCursor(il);

            /*
             * line 719
             * objectOfType1.ChangeLevelServerRpc(node.buyRerouteToMoon, this.groupCredits);
             */
            c.GotoNext(
                x => x.MatchCallvirt<StartOfRound>(nameof(StartOfRound.ChangeLevelServerRpc))
            );

            c.Remove();
            
            c.EmitDelegate<Action<StartOfRound, int, int>>((_, levelId, credits) => {
                var team = Player.Local.Team;
                if (team == null) return;
                
                team.ChangeLevelServerRpc(levelId, credits);
            });
            
            /*
             * line 724
             * objectOfType1.BuyShipUnlockableServerRpc(node.shipUnlockableID, this.groupCredits);
             */
            c.GotoNext(
                x => x.MatchCallvirt<StartOfRound>(nameof(StartOfRound.BuyShipUnlockableServerRpc))
            );

            c.Remove();
            
            c.EmitDelegate<Action<StartOfRound, int, int>>((_, id, credits) => {
                var team = Player.Local.Team;
                if (team == null) return;
                
                team.BuyShipUnlockableServerRpc(id, credits);
            });
            
            /*
            // [738 15 - 738 82]
            c.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<Terminal>(nameof(Terminal.groupCredits)),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<Terminal>(nameof(Terminal.hasWarrantyTicket)),
                x => x.MatchCall<Terminal>(nameof(Terminal.BuyVehicleServerRpc))
            );

            // this
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<Terminal>>(self => {
                var localTeam = Player.Local.Team;
                if (localTeam == null) return;

                localTeam.Credits = self.groupCredits;
            });
            */
        };

        On.Terminal.SyncBoughtVehicleWithServer += (_, self, vehicleID) => {
            var localTeam = Player.Local.Team;
            if (self.IsServer || localTeam == null) {
                return;
            }

            self.useCreditsCooldown = true;
            localTeam.BuyVehicleServerRpc(vehicleID, self.groupCredits, self.hasWarrantyTicket);
            
            // don't call orig
        };
        
        // remove assignments to Terminal.groupCredits
        
        IL.StartOfRound.OnPlayerConnectedClientRpc += il => {
            var c = new ILCursor(il);

            /*
             * Terminal objectOfType1 = UnityEngine.Object.FindObjectOfType<Terminal>();
             * objectOfType1.groupCredits = serverMoneyAmount;
             */
            c.GotoNext(
                x => x.MatchCall<Object>(nameof(FindObjectOfType)),
                x => x.MatchDup()
            );

            // don't remove the first line
            c.GotoNext();
            c.RemoveRange(3);
        };

        IL.StartOfRound.ChangeLevelClientRpc += il => {
            var c = new ILCursor(il);
            
            // [3498 5 - 3498 23]
            c.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchCall<NetworkBehaviour>("get_IsServer"),
                x => x.MatchBrfalse(out _),
                x => x.MatchRet()
            );
            
            c.RemoveRange(7);
        };

        IL.StartOfRound.BuyShipUnlockableClientRpc += il => {
            var c = new ILCursor(il);
            
            c.GotoNext(
                x => x.MatchCall<Object>(nameof(FindObjectOfType)),
                x => x.MatchLdarg(1),
                x => x.MatchStfld<Terminal>(nameof(Terminal.groupCredits))
            );
            
            c.RemoveRange(3);
            
            // there's a label targetting the removed line, so we need to reposition it
            var label = c.MarkLabel();
            
            c.GotoPrev(
                x => x.MatchBeq(out _)
            );

            c.Remove();
            c.Emit(OpCodes.Beq_S, label);
        };

        IL.Terminal.BuyVehicleClientRpc += il => {
            var c = new ILCursor(il);

            c.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdarg(1),
                x => x.MatchStfld<Terminal>(nameof(Terminal.groupCredits))
            );

            c.RemoveRange(3);
        };

        IL.Terminal.SyncTerminalValuesClientRpc += il => {
            var c = new ILCursor(il);

            c.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdarg(1),
                x => x.MatchStfld<Terminal>(nameof(Terminal.groupCredits))
            );

            c.RemoveRange(3);
        };

        IL.TimeOfDay.SyncNewProfitQuotaClientRpc += il => {
            var c = new ILCursor(il);
            
            c.GotoNext(
                x => x.MatchLdloc(0),
                x => x.MatchLdloc(0),
                x => x.MatchLdfld<Terminal>(nameof(Terminal.groupCredits)),
                x => x.MatchLdarg(2),
                x => x.MatchAdd(),
                x => x.MatchLdloc(0),
                x => x.MatchLdfld<Terminal>(nameof(Terminal.groupCredits)),
                x => x.MatchLdcI4(100_000_000),
                x => x.MatchCall(typeof(Mathf), nameof(Mathf.Clamp)),
                x => x.MatchStfld<Terminal>(nameof(Terminal.groupCredits))
            );
            
            c.RemoveRange(10);
        };
    }
}