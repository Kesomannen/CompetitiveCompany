using System;
using System.Collections.Generic;
using System.Linq;
using CompetitiveCompany.Game;
using LethalAPI.LibTerminal;
using LethalAPI.LibTerminal.Attributes;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CompetitiveCompany;

internal class TerminalCommands {
    const string NoPermsMessage = "You do not have permission to use this command!";
    const string AlreadyPlayingMessage = "You cannot use this command while a round is active!";

    TerminalCommands() { }
    
    internal static void Initialize() {
        TerminalRegistry.CreateTerminalRegistry().RegisterFrom(new TerminalCommands());
    }
    
    [
        TerminalCommand("settings", clearText: true),
        CommandInfo("Show the current game settings.")
    ]
    string SettingsCommand() {
        var settings = Session.Current.Settings;

        return $"Current game settings:\n\n" +
               $"Friendly fire: {FormatBool(settings.FriendlyFire)}\n" +
               $"Number of rounds: {settings.NumberOfRounds}\n" +
               $"Ship safe radius: {settings.ShipSafeRadius}m";
        
        string FormatBool(bool value) => value ? "Enabled" : "Disabled";
    }
    
    /*
    [
        TerminalCommand("spectate"),
        CommandInfo("Leave your team and start spectating the game.")
    ]
    string SpectateCommand() {
        Player.Local.StartSpectating();
        return "You are now spectating.";
    }
    */

    [
        TerminalCommand("join", clearText: true),
        CommandInfo("Join a team. Host can join other players by specifying their username after the team name.", "<team> [player]")
    ]
    string JoinCommand([RemainingText] string input) {
        if (Session.Current.IsRoundActive) return AlreadyPlayingMessage;
        if (!CheckPerms(settings => settings.JoinTeamPerm)) return NoPermsMessage;
        
        return NetworkManager.Singleton.IsServer switch {
            true => Server(),
            false => Client()
        };

        string Server() {
            // both the team name and player name might have spaces in them,
            // so we check each possible "break point" in between the arguments
            
            // example:
            // input = "team name playerName"
            
            // i = 0
            // teamName = "team", playerName = "name playerName"
            // -- team could not be found, continue
            
            // i = 1
            // teamName = "team name", playerName = "playerName"
            // -- team and player found, break
            
            var spaceIndicies = input
                .Select((c, i) => (c, i))
                .Where(t => t.c == ' ')
                .Select(t => t.i)
                .Append(input.Length)
                .ToArray();
            
            string? playerName = null;
            Player? player = null;
            Team? team = null;
            
            for (var i = 0; i < spaceIndicies.Length; i++) {
                var teamName = input[..spaceIndicies[i]];
                
                if (!Session.Current.Teams.TryGet(teamName, out team)) {
                    continue;
                }
                
                if (i == spaceIndicies.Length - 1) {
                    // no player was specified, default to local
                    player = Player.Local;
                    break;
                }

                playerName = input[(spaceIndicies[i] + 1)..];
                if (Session.Current.Players.TryGetByName(playerName, out player)) {
                    break;
                }
            }
            
            if (team == null || player == null) {
                return playerName != null ?
                    $"Player '{playerName}' not found!" :
                    "Invalid team or player name!";
            }
            
            if (player.Team == team) {
                return player.IsOwner ? 
                    $"You are already in team {team.Name}!" :
                    $"Player {playerName} is already in team {team.Name}!";
            }
            
            player.SetTeamFromServer(team);
            return player.IsOwner ? 
                $"Joined team {team.Name}." :
                $"Moved {playerName} to team {team.RawName}";
        }

        string Client() {
            if (!Session.Current.Teams.TryGet(input, out var team)) {
                return $"Team '{input}' not found!";
            }

            var player = Player.Local;
            if (player.Team == team) {
                return $"You are already in team {team.Name}!";
            }
            
            player.SetTeamServerRpc(team);
            return $"Joined team {team.RawName}.";
        }
    }

    [
        TerminalCommand("create-team", clearText: true),
        CommandInfo("Create a new team", "<name>")
    ]
    string CreateTeam([RemainingText] string teamName) {
        if (Session.Current.IsRoundActive) return AlreadyPlayingMessage;
        if (!CheckPerms(settings => settings.CreateAndDeleteTeamPerm)) return NoPermsMessage;

        if (Session.Current.Teams.Count >= Session.MaxTeams) {
            return "Maximum number of teams reached!";
        }

        if (Session.Current.Teams.TryGet(teamName, out _)) {
            return $"Team {teamName} already exists!";
        }

        if (teamName.Length > 128) {
            return "Team name is too long!";
        }

        var color = Random.ColorHSV(0, 1, 1, 1, 1, 1);
        Session.Current.CreateTeamServerRpc(teamName, color);
        return $"Created team {teamName}.";
    }

    [
        TerminalCommand("delete-team", clearText: true),
        CommandInfo("Delete a team", "<team>")
    ]
    string DeleteTeam([RemainingText] string teamName) {
        if (Session.Current.IsRoundActive) return AlreadyPlayingMessage;
        if (!CheckPerms(settings => settings.CreateAndDeleteTeamPerm)) return NoPermsMessage;

        if (Session.Current.Teams.Count <= Session.MinTeams) {
            return "Minimum number of teams reached!";
        }

        if (!Session.Current.Teams.TryGet(teamName, out var team)) {
            return $"Team {teamName} not found!";
        }

        if (team.Members.Any()) {
            return $"Team {teamName} still has members!";
        }

        team.DeleteServerRpc();
        return $"Deleted team {teamName}.";
    }

    [
        TerminalCommand("list-teams", clearText: true),
        CommandInfo("Lists all teams")
    ]
    string ListTeams() {
        return string.Join('\n', Session.Current.Teams.PrettyPrint(51, color: true));
    }

    [
        TerminalCommand("set-team-color", clearText: true),
        CommandInfo("Set the color of your current team", "<color>")
    ]
    string ColorCommand([RemainingText] string input) {
        if (Session.Current.IsRoundActive) return AlreadyPlayingMessage;
        if (!CheckPerms(settings => settings.EditTeamPerm)) return NoPermsMessage;

        var team = Player.Local.Team;

        if (team == null) {
            return "You are not in a team!";
        }

        var color = ParseColor(input);
        if (color == null) {
            return $"Invalid color! Please use hex format (e.g. #FF0000) or one of the following: {string.Join(", ", _colorNames.Keys)}.";
        }

        team.SetColorServerRpc(color.Value);
        var hex = input.StartsWith("#") ? input : "#" + ColorUtility.ToHtmlStringRGB(color.Value);
        return $"Set color of team {team.Name} to <color={hex}>{input}</color>.";
    }
    
    static readonly Dictionary<string, Color> _colorNames = new() {
        { "red", Color.red },
        { "green", Color.green },
        { "blue", Color.blue },
        { "yellow", Color.yellow },
        { "cyan", Color.cyan },
        { "magenta", Color.magenta },
        { "white", Color.white },
        { "black", Color.black }
    };

    static Color? ParseColor(string str) {
        if (_colorNames.TryGetValue(str, out var color)) {
            return color;
        }
        
        if (ColorUtility.TryParseHtmlString(str, out color)) {
            return color;
        }
        
        return null;
    }

    [
        TerminalCommand("set-team-name", clearText: true),
        CommandInfo("Renames your current team", "<name>")
    ]
    string RenameCommand([RemainingText] string name) {
        if (Session.Current.IsRoundActive) return AlreadyPlayingMessage;
        if (!CheckPerms(settings => settings.EditTeamPerm)) return NoPermsMessage;

        var team = Player.Local.Team;

        if (team == null) {
            return "You are not in a team!";
        }

        if (Session.Current.Teams.TryGet(name, out _)) {
            return $"Team {name} already exists!";
        }

        if (name.Length > 64) {
            return "Team name is too long!";
        }

        team.SetNameServerRpc(name);
        return $"Renamed team to {name}.";
    }

    static bool CheckPerms(Func<NetworkedSettings, Permission> getPerms) {
        var settings = Session.Current.Settings;
        return NetworkManager.Singleton.IsHost || getPerms(settings) == Permission.Everyone;
    }
}