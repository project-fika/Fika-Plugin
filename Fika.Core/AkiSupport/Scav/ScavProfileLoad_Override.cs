using Aki.Reflection.Patching;
using EFT;
using System.Reflection;

namespace Fika.Core.AkiSupport.Scav
{
    internal class ScavProfileLoad_Override : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(TarkovApplication).GetMethod(nameof(TarkovApplication.method_46));

        [PatchPrefix]
        private static void PatchPrefix(ref string profileId, Profile savageProfile, RaidSettings ____raidSettings)
        {
            if (!____raidSettings.IsPmc)
            {
                profileId = savageProfile.Id;
            }
        }
    }
}