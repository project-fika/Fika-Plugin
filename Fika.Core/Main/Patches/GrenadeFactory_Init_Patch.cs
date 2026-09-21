using EFT.InventoryLogic;
using System.Reflection;
using Comfort.Common;
using Fika.Core.Main.GameMode;
using SPTushonka.Reflection.Patching;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.Patches;

/// <summary>
/// Used to help us keep track of thrown grenades during a session for kill progression
/// </summary>
public class GrenadeFactory_Init_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(GrenadeFactory)
            .GetMethod(nameof(GrenadeFactory.Create));
    }

    [PatchPostfix]
    public static void Postfix(ThrowWeap item)
    {
        var fikaGame = FikaGlobals.FikaGame;
        if (fikaGame != null)
        {
            fikaGame.GameController.ThrownGrenades.Add(item);
        }
    }
}
