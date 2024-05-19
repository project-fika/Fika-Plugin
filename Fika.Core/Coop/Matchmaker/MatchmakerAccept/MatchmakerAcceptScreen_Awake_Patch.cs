using Aki.Reflection.Patching;
using Aki.Reflection.Utils;
using EFT.UI.Matchmaker;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Reflection;

namespace Fika.Core.Coop.Matchmaker
{
    public class MatchmakerAcceptScreen_Awake_Patch : ModulePatch
    {
        [Serializable]
        private class ServerStatus
        {
            [JsonProperty("ip")]
            public string ip { get; set; }

            [JsonProperty("status")]
            public string status { get; set; }
        }

        //static BindingFlags privateFlags = BindingFlags.NonPublic | BindingFlags.Instance;

        public static Type GetThisType() => PatchConstants.EftTypes.Single(x => x == typeof(MatchMakerAcceptScreen));

        protected override MethodBase GetTargetMethod() => typeof(MatchMakerAcceptScreen).GetMethod("Awake");

        [PatchPrefix]
        private static bool PatchPrefix(MatchMakerAcceptScreen __instance, PlayersRaidReadyPanel ____playersRaidReadyPanel, MatchMakerGroupPreview ____groupPreview)
        {
            MatchmakerAcceptPatches.MatchMakerAcceptScreenInstance = __instance;
            MatchmakerAcceptPatches.PlayersRaidReadyPanel = ____playersRaidReadyPanel;
            MatchmakerAcceptPatches.MatchMakerGroupPreview = ____groupPreview;
            return true;
        }

    }
}









