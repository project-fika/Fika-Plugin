using EFT;
using Fika.Core.Coop.GameMode;
using Fika.Core.Patching;
using System.Collections.Generic;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
    /// <summary>
    /// Used to prevent players from getting everyone elses BTR items
    /// </summary>
    public class BaseLocalGame_method_13_Patch : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BaseLocalGame<EftGamePlayerOwner>).GetMethod(nameof(BaseLocalGame<EftGamePlayerOwner>.method_13));
        }

        [PatchPrefix]
        public static bool Prefix(BaseLocalGame<EftGamePlayerOwner> __instance, ref Dictionary<string, GClass1356[]> __result)
        {
            if (__instance is CoopGame coopGame)
            {
                __result = coopGame.GetOwnSentItems(coopGame.ProfileId);
                return false;
            }
            return true;
        }
    }
}
