using System;
using System.Collections.Generic;
using System.Linq;
using CompetitiveCompany.Util;
using MonoMod.Cil;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

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
        set => _credits.Value = value;
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

    public event Action<Color>? OnColorChanged;
    public event Action<string>? OnNameChanged; 
    public event Action<int>? OnCreditsChanged; 
    
    void Start() {
        Suit = SuitHelper.CreateSuit("CompetitiveCompanySuit", StartOfRound.Instance);
        
        SuitMaterial = Suit.suitMaterial;
        SuitMaterial.mainTexture = null;
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
    /// Deletes the team. Fails if there are still members in the team.
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
    void SyncBoughtItemsServerRpc(int[] boughtItems, int newTeamCredits, int numItemsInShip) {
        Log.Debug($"{RawName}: Syncing bought items to clients. New credits: {newTeamCredits}, items in ship: {numItemsInShip}");
        
        TerminalUtil.Instance.orderedItemsFromTerminal.AddRange(boughtItems);
        Credits = newTeamCredits;
        SyncItemsInShipClientRpc(numItemsInShip);
    }
    
    [ClientRpc]
    void SyncItemsInShipClientRpc(int itemsInShip) {
        Log.Debug($"Received items in ship from server: {itemsInShip}");
        TerminalUtil.Instance.numberOfItemsInDropship = itemsInShip;
    }

    internal static void Patch() {
        // instead of using the normal SyncBoughtItemsServerRpc, which sets the groupCredits for
        // all clients, we use our own method that only does it for the local team
        IL.Terminal.SyncBoughtItemsWithServer += il => {
            var c = new ILCursor(il);

            c.GotoNext(
                x => x.MatchCall<Terminal>(nameof(Terminal.BuyItemsServerRpc))
            );

            c.Remove();

            c.EmitDelegate<Action<Terminal, int[], int, int>>((_, boughtItems, newGroupCredits, numItemsInShip) => {
                var team = Player.Local.Team;
                if (team == null) return;
                
                team.SyncBoughtItemsServerRpc(boughtItems, newGroupCredits, numItemsInShip);
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
    }
}