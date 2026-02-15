using System.Reflection;
using HarmonyLib;
using SPT.Custom.CustomAI;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.Overrides;

internal class PmcFoundInRaidEquipment_ConfigurePMCFindInRaidStatus_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(PmcFoundInRaidEquipment),
            nameof(PmcFoundInRaidEquipment.ConfigurePMCFindInRaidStatus));
    }

    [PatchPrefix]
    public static bool Prefix()
    {
        return false;
    }
}
