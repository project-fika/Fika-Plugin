using System.Collections.Generic;
using System.Reflection;
using EFT;
using Fika.Core.Main.GameMode;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.LocalGame;

/// <summary>
/// Used to prevent players from getting everyone elses BTR items
/// </summary>
public class BaseLocalGame_method_14_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(BaseLocalGame<EftGamePlayerOwner>)
            .GetMethod(nameof(BaseLocalGame<EftGamePlayerOwner>.method_14));
    }

    [PatchPrefix]
    public static bool Prefix(BaseLocalGame<EftGamePlayerOwner> __instance, ref Dictionary<string, FlatItemsDataClass[]> __result)
    {
        if (__instance is CoopGame coopGame)
        {
            __result = coopGame.GetOwnSentItems(coopGame.ProfileId);
            return false;
        }
        return true;
    }
}
