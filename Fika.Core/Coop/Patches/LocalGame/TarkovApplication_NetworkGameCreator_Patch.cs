using EFT;
using SPT.Reflection.Patching;
using System.Reflection;
using System.Threading.Tasks;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Utils;

namespace Fika.Core.Coop.Patches.LocalGame
{
    internal class TarkovApplication_NetworkGameCreator_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.GetDeclaredMethods(typeof(TarkovApplication)).SingleCustom(m =>
                m.Name == nameof(TarkovApplication.method_39) && m.GetParameters().Length == 2);
        }

        [PatchPrefix]
        public static bool Prefix(TarkovApplication __instance, RaidSettings ____raidSettings, string groupId, EMatchingType type, ref Task __result)
        {
            // Assuming method_38 is now async and returns Task
            __result = __instance.method_38(____raidSettings.TimeAndWeatherSettings);

            // Log and handle other necessary parts from method_39
            FikaPlugin.Instance.FikaLogger.LogDebug("Matching with group id: " + groupId);
            if (MonoBehaviourSingleton<PreloaderUI>.Instantiated)
                MonoBehaviourSingleton<PreloaderUI>.Instance.MenuTaskBar.PreparingRaid();

            // Return false to skip the original method_39
            return false;
        }
    }
}