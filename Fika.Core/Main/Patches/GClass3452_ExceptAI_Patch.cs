using Comfort.Common;
using EFT;
using Fika.Core.Networking;
using Fika.Core.Patching;
using System.Collections.Generic;
using System.Reflection;

namespace Fika.Core.Main.Patches
{
    public class GClass3452_ExceptAI_Patch : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass3452)
                .GetMethod(nameof(GClass3452.ExceptAI));
        }

        [PatchPrefix]
        public static bool Prefix(IEnumerable<IPlayer> persons, ref IEnumerable<IPlayer> __result)
        {
            if (persons != null)
            {
                __result = Singleton<IFikaNetworkManager>.Instance.CoopHandler.HumanPlayers;
                return false;
            }

            return true;
        }
    }
}
