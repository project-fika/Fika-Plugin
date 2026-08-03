using EFT.Game.Spawning;
using System.Collections.Generic;
using System.Reflection;
using Comfort.Common;
using EFT;
using Fika.Core.Networking;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches;

public class PlayersCollectionExtension_ExceptAI_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(PlayersCollectionExtension)
            .GetMethod(nameof(PlayersCollectionExtension.ExceptAI));
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
