using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace CompetitiveCompany.Game;

[HarmonyPatch(typeof(HUDManager), nameof(HUDManager.AddChatMessage))]
internal static class ChatPatches {
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
        Log.Debug("Patching HUDManager.AddChatMessage...");
        
        foreach (var instruction in instructions) {
            // find "if (string.IsNullOrEmpty(nameOfUserWhoTyped))" line
            if (instruction.opcode == OpCodes.Ldarg_2) {
                // ChatPatcher.HandleChatMessage(this, chatMessage, playerNameWhoSent);
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Ldarg_1);
                yield return new CodeInstruction(OpCodes.Ldarg_2);
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ChatPatches), nameof(HandleChatMessage)));
                
                // return;
                yield return new CodeInstruction(OpCodes.Ret);
                yield break;
            }
            
            yield return instruction;
        }
        
        Log.Error("Failed to find target instruction in HUDManager.AddChatMessage!");
    }
    
    static readonly Color _noTeamColor = Color.red;

    static void HandleChatMessage(HUDManager instance, string chatMessage, string playerNameWhoSent) {
        string str;
        if (string.IsNullOrEmpty(playerNameWhoSent)) {
            str = $"<color=#7069ff>{chatMessage}</color>";
        } else {
            var localTeam = Player.Local.Team;
            var otherTeam = Session.Current.Players.GetByName(playerNameWhoSent)?.Team;

            if (localTeam != null && otherTeam != null && localTeam != otherTeam) {
                return;
            }

            var htmlColor = ColorUtility.ToHtmlStringRGB(otherTeam?.Color ?? _noTeamColor);
            
            var shownName = playerNameWhoSent;
            if (otherTeam != null) {
                shownName += $" ({otherTeam.Name})";
            }
            str = $"<color=#{htmlColor}>{shownName}</color>: <color=#FFFF00>'{chatMessage}'</color>";
        }

        instance.ChatMessageHistory.Add(str);
        instance.chatText.text = string.Join('\n', instance.ChatMessageHistory);
    }
}