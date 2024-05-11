using Aki.Reflection.Patching;
using EFT;
using EFT.UI;
using EFT.UI.Matchmaker;
using Fika.Core.UI.Custom;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Fika.Core.Coop.Matchmaker
{
    public class MatchmakerAcceptScreenShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(MatchMakerAcceptScreen).GetMethods(BindingFlags.Public | BindingFlags.Instance).First(x => x.Name == "Show" && x.GetParameters()[0].Name == "session");

        private static GameObject MatchmakerObject { get; set; }

        [PatchPrefix]
        private static void PreFix(ref ISession session, ref RaidSettings raidSettings, Profile ___profile_0, MatchMakerAcceptScreen __instance,
            DefaultUIButton ____acceptButton, DefaultUIButton ____backButton, MatchMakerPlayerPreview ____playerModelView)
        {
            if (MatchmakerObject == null)
            {
                MatchmakerObject = new GameObject("MatchmakerObject");
            }

            if (raidSettings.Side == ESideType.Savage)
            {
                raidSettings.RaidMode = ERaidMode.Local;
            }

            MatchMakerUIScript newMatchMaker = MatchmakerObject.GetOrAddComponent<MatchMakerUIScript>();
            newMatchMaker.RaidSettings = raidSettings;
            newMatchMaker.AcceptButton = ____acceptButton;
            newMatchMaker.BackButton = ____backButton;
        }

        [PatchPostfix]
        private static void PostFix(ref ISession session, Profile ___profile_0, MatchMakerAcceptScreen __instance)
        {
            MatchmakerAcceptPatches.MatchMakerAcceptScreenInstance = __instance;
            MatchmakerAcceptPatches.Profile = ___profile_0;
            MatchmakerAcceptPatches.PMCName = session.Profile.Nickname;
        }
    }


}
