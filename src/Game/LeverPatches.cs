using System;
using System.Linq;
using CompetitiveCompany.Util;
using MonoMod.Cil;

namespace CompetitiveCompany.Game;

internal static class LeverPatches {
    public static void Patch() {
        IL.StartMatchLever.Update += il => {
            var c = new ILCursor(il);

            c.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<StartMatchLever>(nameof(StartMatchLever.triggerScript)),
                x => x.MatchLdstr("Start ship : [LMB]"),
                x => x.MatchStfld<InteractTrigger>(nameof(InteractTrigger.hoverTip)),
                x => x.MatchRet()
            );
            
            c.GotoNext();
            c.RemoveRange(4);
            
            c.EmitDelegate<Action<StartMatchLever>>(self => {
                var canLeave = TimeUtil.GetCurrentGameTime().TotalHours >= Session.Current.Settings.MinLeaveTime;
                self.triggerScript.hoverTip = canLeave ? "Start ship : [LMB]" : "[Too early to start ship]";
                self.triggerScript.interactable = canLeave;
            });
        };

        On.HUDManager.DisplaySpectatorVoteTip += (orig, self) => {
            if (!Session.Current.Settings.DisableAutoPilot) {
                orig(self);
            }
        };

        On.HUDManager.Update += (orig, self) => {
            orig(self);

            if (Session.Current.Settings.DisableAutoPilot) {
                self.holdButtonToEndGameEarlyHoldTime = 0;
            }
        };

        On.StartMatchLever.EndGame += (orig, self) => {
            var startOfRound = StartOfRound.Instance;
            
            if (
                !Player.Local.Controller.isPlayerDead &&
                !startOfRound.shipHasLanded ||
                startOfRound.shipIsLeaving ||
                startOfRound.shipLeftAutomatically
            ) {
                return;
            }
            
            var session = Session.Current;
            
            if (session.Settings.TimeToLeave <= 0 || session.Teams.GetLiving().Count() == 1) {
                orig(self); // vanilla behaviour
                return;
            }
            
            var localTeam = Player.Local.Team;
            if (localTeam == null) {
                Log.Warning("Player without a team pulled the lever!");
                return;
            }
            
            localTeam.StartLeavingServerRpc();
        };
    }
}