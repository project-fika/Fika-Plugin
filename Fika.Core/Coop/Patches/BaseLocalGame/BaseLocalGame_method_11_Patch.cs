using System.Reflection;
using SPT.Reflection.Patching;
using EFT;
using Fika.Core.Coop.Utils;

namespace Fika.Core.Coop.Patches
{
    internal class BaseLocalGame_method_11_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BaseLocalGame<EftGamePlayerOwner>).GetMethod(nameof(BaseLocalGame<EftGamePlayerOwner>.method_11));
        }

        [PatchPrefix]
        public static bool PatchPrefix(ref LocationSettingsClass.Location location)
        {
            if (FikaBackendUtils.IsClient && FikaBackendUtils.IsReconnect)
            {
                location.Loot = FikaBackendUtils.ReconnectPacket.Value.Items;
            }

            return true;
        }
    }
}