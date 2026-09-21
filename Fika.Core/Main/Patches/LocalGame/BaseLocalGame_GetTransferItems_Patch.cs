using Fika.Core.Main.Utils;
using System.Collections.Generic;
using System.Reflection;
using EFT;
using Fika.Core.Main.GameMode;
using SPTushonka.Reflection.Patching;
using JsonType;

namespace Fika.Core.Main.Patches.LocalGame;

/// <summary>
/// Used to prevent players from getting everyone elses BTR items
/// </summary>
public class BaseLocalGame_GetTransferItems_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(BaseLocalGame<EftGamePlayerOwner>)
            .GetMethod(nameof(BaseLocalGame<EftGamePlayerOwner>.GetTransferItems));
    }

    [PatchPrefix]
    public static bool Prefix(BaseLocalGame<EftGamePlayerOwner> __instance, ref Il2CppSystem.Collections.Generic.Dictionary<string, Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<FlatItem>> __result)
    {
        if (__instance is CoopGame coopGame)
        {
            __result = coopGame.GetOwnSentItems(coopGame.ProfileId);
            return false;
        }
        return true;
    }
}
