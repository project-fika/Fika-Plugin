using Aki.Reflection.Patching;
using EFT;
using System.Reflection;

namespace Fika.Core.UI
{
    internal class OfflineDisplayProgressPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(TarkovApplication).GetMethod(nameof(TarkovApplication.method_47));

        [PatchPrefix]
        public static bool PatchPrefix(ref RaidSettings ____raidSettings)
        {
            ____raidSettings.RaidMode = ERaidMode.Local;
            return true;
        }

        [PatchPostfix]
        public static void PatchPostfix(ref RaidSettings ____raidSettings)
        {
            ____raidSettings.RaidMode = ERaidMode.Local;
        }
    }
}
