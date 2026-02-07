using System.Collections.Generic;
using System.Reflection;
using Comfort.Common;
using EFT;
using Fika.Core.Networking;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches;

public class GClass3585_ExceptAI_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(GClass3585)
            .GetMethod(nameof(GClass3585.ExceptAI));
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
