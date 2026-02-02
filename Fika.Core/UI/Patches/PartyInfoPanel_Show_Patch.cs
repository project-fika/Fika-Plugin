using System.Reflection;
using EFT.UI.Matchmaker;
using Fika.Core.Main.Utils;
using SPT.Reflection.Patching;

namespace Fika.Core.UI.Patches;

public class PartyInfoPanel_Show_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(PartyInfoPanel)
            .GetMethod(nameof(PartyInfoPanel.Show));
    }

    [PatchPrefix]
    public static void Prefix(ref GClass1628<GroupPlayerViewModelClass> groupPlayers)
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
