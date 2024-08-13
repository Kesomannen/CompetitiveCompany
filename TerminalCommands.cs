using System;
using System.Collections.Generic;
using System.Linq;
using CompetitiveCompany.Game;
using CompetitiveCompany.Util;
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
    
    [TerminalCommand("spectate"), CommandInfo("Spectate the game")]
    string SpectateCommand() {
        Player.Local.StartSpectating();
        return "You are now spectating.";
    }

    [
        TerminalCommand("join", clearText: true),
        CommandInfo("Join a team. Host can join other players by specifying their username.", "[player] <team>")
    ]
    string JoinCommand([RemainingText] string teamName) {
        if (Session.Current.IsRoundActive) return AlreadyPlayingMessage;
        if (!CheckPerms(settings => settings.JoinTeamPerm)) return NoPermsMessage;

        if (!Session.Current.Teams.TryGet(teamName, out var team)) {
            return $"Team '{teamName}' not found!";
        }

        if (Player.Local.Team?.Name == teamName) {
            return $"You are already in team {teamName}!";
        }

        Player.Local.SetTeamServerRpc(team);
        return $"Joined team {team.Name}.";
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
        CommandInfo("Set the color of your current team, in hex format", "<color>")
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
            return $"Invalid color! Please use hex format (e.g. #FF0000) or one of the following: {string.Join(", ", _colorNames)}.";
        }

        team.SetColorServerRpc(color.Value);
        return $"Set color of team {team.Name} to <color={ColorUtility.ToHtmlStringRGB(color.Value)}>{input}</color>.";
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