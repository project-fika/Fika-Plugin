using EFT.UI;
using Fika.Core.Coop.Utils;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Fika.Core.Coop.FreeCamera.Patches
{
    public class PlayEndGameSound_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GUISounds).GetMethod(nameof(GUISounds.PlayEndGameSound), [typeof(EEndGameSoundType)]);
        }

        [PatchPrefix]
        private static bool Prefix()
        {
            // Don't play end game sound if spectator mode
            if (FikaBackendUtils.IsSpectator)
            {
                return false;
            }

            return true;
        }
    }
}
