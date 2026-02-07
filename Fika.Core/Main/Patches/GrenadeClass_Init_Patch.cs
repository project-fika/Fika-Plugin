using System.Reflection;
using Comfort.Common;
using Fika.Core.Main.GameMode;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches;

/// <summary>
/// Used to help us keep track of thrown grenades during a session for kill progression
/// </summary>
public class GrenadeClass_Init_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(GrenadeFactoryClass)
            .GetMethod(nameof(GrenadeFactoryClass.Create));
    }

    [PatchPostfix]
    public static void Postfix(ThrowWeapItemClass item)
    {
        var fikaGame = Singleton<IFikaGame>.Instance;
        if (fikaGame != null)
        {
            fikaGame.GameController.ThrownGrenades.Add(item);
        }
    }
}
