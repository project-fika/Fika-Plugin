using EFT;
using EFT.InventoryLogic;
using Fika.Core.Patching;
using System.Reflection;

namespace Fika.Core.Main.Patches
{
    /// <summary>
    /// This patch aims to alleviate problems with bots refilling mags too quickly causing a desync on <see cref="Item.Id"/>s by blocking firing for 0.7s
    /// </summary>
    public class BotReload_method_1_Patch : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotReload)
                .GetMethod(nameof(BotReload.method_1));
        }

        [PatchPostfix]
        public static void Postfix(BotOwner ___BotOwner_0)
        {
            ___BotOwner_0.ShootData.BlockFor(0.7f);
        }
    }
}
