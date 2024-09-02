using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using UnityEngine;

namespace CompetitiveCompany.Game;

internal static class MiscPatches {
    public static void Patch() {
        NoCompanyMoonPatch();
        
        On.TimeOfDay.UpdateProfitQuotaCurrentTime += (_, _) => {
            var startOfRound = StartOfRound.Instance;
            // this might run before the local player controller is spawned
            if (startOfRound.localPlayerController == null) return;
            var session = Session.Current;

            var roundsLeft = session.Settings.NumberOfRounds - Mathf.Max(session.RoundNumber, 0);
            startOfRound.deadlineMonitorText.text = roundsLeft == 1 ? "DEADLINE: LAST ROUND!" : $"DEADLINE: {roundsLeft} ROUNDS LEFT";
            
            var localTeam = Player.Local.Team;

            startOfRound.profitQuotaMonitorText.text = localTeam == null ? 
                "TEAM CREDITS: N/A" :
                $"TEAM CREDITS: ${localTeam.Credits}";
        };
    }

    static TerminalNode? _refuseCompanyMoonNode;
    
    static void NoCompanyMoonPatch() {
        _refuseCompanyMoonNode = ScriptableObject.CreateInstance<TerminalNode>();
        _refuseCompanyMoonNode.displayText = "Company moon is disabled by CompetitiveCompany.";
        _refuseCompanyMoonNode.clearPreviousText = true;

        // prevent going to the company moon
        IL.Terminal.LoadNewNodeIfAffordable += il => {
            var c = new ILCursor(il);
            
            /*
             * [719 13 - 719 89]
             * this.useCreditsCooldown = true;
             * objectOfType1.ChangeLevelServerRpc(node.buyRerouteToMoon, this.groupCredits);
            */
            c.GotoNext(
                x => x.MatchLdloc(0),
                x => x.MatchLdarg(1),
                x => x.MatchLdfld<TerminalNode>(nameof(TerminalNode.buyRerouteToMoon)),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<Terminal>(nameof(Terminal.groupCredits)),
                x => x.MatchCallvirt<StartOfRound>(nameof(StartOfRound.ChangeLevelServerRpc))
            );

            var target = c.Next;
            
            /*
             * if (node.buyRerouteToMoon == 3) {
             *     this.LoadNewNode(Session._refuseCompanyMoonNode);
             *     return;
             * }
             */
            
            // node
            c.Emit(OpCodes.Ldarg_1);
            // buyRerouteToMoon
            c.Emit(OpCodes.Ldfld, AccessTools.Field(typeof(TerminalNode), nameof(TerminalNode.buyRerouteToMoon)));
            // 3 -- id of company moon
            c.Emit(OpCodes.Ldc_I4_3);
            // if (node.buyRerouteToMoon != 3) goto next
            c.Emit(OpCodes.Bne_Un_S, target);
            
            // this
            c.Emit(OpCodes.Ldarg_0);
            // MiscPatches._refuseCompanyMoonNode
            c.Emit(OpCodes.Ldsfld, AccessTools.Field(typeof(MiscPatches), nameof(_refuseCompanyMoonNode)));
            // LoadNewNode
            c.Emit(OpCodes.Call, AccessTools.Method(typeof(Terminal), nameof(Terminal.LoadNewNode)));
            
            // return
            c.Emit(OpCodes.Ret);
        };
    }
}