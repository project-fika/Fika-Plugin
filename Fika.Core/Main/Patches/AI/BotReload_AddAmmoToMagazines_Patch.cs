using System.Reflection;
using EFT;
using EFT.InventoryLogic;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.AI;

/// <summary>
/// This patch aims to alleviate problems with bots refilling mags too quickly causing a desync on <see cref="Item.Id"/>s by blocking firing for 0.7s
/// </summary>
public class BotReload_AddAmmoToMagazines_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(BotReload)
            .GetMethod(nameof(BotReload.AddAmmoToMagazines));
    }

    [PatchPostfix]
    public static void Postfix(BotOwner ___BotOwner_0)
    {
        ___BotOwner_0.ShootData.BlockFor(0.7f);
    }
}
