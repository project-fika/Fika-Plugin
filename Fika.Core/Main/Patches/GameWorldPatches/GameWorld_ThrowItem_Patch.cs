using System.Linq;
using System.Reflection;
using EFT;
using EFT.Interactive;
using Fika.Core.Main.Components;
using Fika.Core.Main.Utils;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.Main.Patches.GameWorldPatches;

public sealed class GameWorld_ThrowItem_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(GameWorld).GetMethods()
            .First(x => x.Name == nameof(GameWorld.ThrowItem) && x.GetParameters().Length == 3);
    }

    [PatchPostfix]
    public static void Postfix(LootItem __result, IPlayer player)
    {
        if (__result is ObservedLootItem observedLootItem)
        {
            if (player.IsYourPlayer || player.IsAI)
            {
                ItemPositionSyncer.Create(observedLootItem.gameObject, FikaBackendUtils.IsServer, observedLootItem);
                observedLootItem._isNetworkGame = false;
                return;
            }

            observedLootItem._isNetworkGame = true;
        }
    }
}
