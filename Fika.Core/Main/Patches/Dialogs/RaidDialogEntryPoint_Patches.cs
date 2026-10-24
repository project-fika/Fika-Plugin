using System.Reflection;
using EFT.Interactive;
using Fika.Core.Main.Components;
using HarmonyLib;
using SPTushonka.Reflection.Patching;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.Patches.Dialogs;

public static class RaidDialogEntryPoint_Patches
{
    public class RaidDialogEntryPoint_Init_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(RaidDialogEntryPoint), nameof(RaidDialogEntryPoint.Init));
        }

        [PatchPostfix]
        public static void Postfix()
        {
            if (FikaBackendUtils.IsTutorial)
            {
                return;
            }

            DialogEntryOccupancy.OnEntryPointInit();
        }
    }
    
    public class RaidDialogEntryPoint_Interact_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(RaidDialogEntryPoint), "_InteractionStates_b__9_0");
        }

        [PatchPrefix]
        public static bool Prefix(RaidDialogEntryPoint __instance)
        {
            if (FikaBackendUtils.IsTutorial)
            {
                return true;
            }

            return DialogEntryOccupancy.TryStartDialog(__instance);
        }
    }
}
