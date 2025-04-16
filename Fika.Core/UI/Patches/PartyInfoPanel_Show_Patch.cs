using EFT.UI.Matchmaker;
using Fika.Core.Coop.Utils;
using Fika.Core.Patching;
using System.Reflection;

namespace Fika.Core.UI.Patches
{
    public class PartyInfoPanel_Show_Patch : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(PartyInfoPanel).GetMethod(nameof(PartyInfoPanel.Show));
        }

        [PatchPrefix]
        public static void Prefix(ref GClass3868<GClass1341> groupPlayers)
        {
            if (groupPlayers != FikaBackendUtils.GroupPlayers && FikaBackendUtils.GroupPlayers.Count > 0)
            {
                groupPlayers = FikaBackendUtils.GroupPlayers;
            }
        }

        [PatchPostfix]
        public static void Postfix(PartyInfoPanel __instance)
        {
            __instance.SetEquipmentViewClicksAvailable(true);
        }
    }
}
