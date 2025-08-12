using EFT;
using Fika.Core.Patching;
using Fika.Core.UI.Custom;
using System.Reflection;

namespace Fika.Core.UI.Patches;

public class HideoutPlayerOwner_SetPointOfView_Patch : FikaPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(HideoutPlayerOwner).GetMethod(nameof(HideoutPlayerOwner.SetPointOfView));
    }

    [PatchPostfix]
    public static void Postfix(HideoutPlayerOwner __instance)
    {
        if (__instance.FirstPersonMode && MainMenuUIScript.Exist)
        {
            MainMenuUIScript.Instance.UpdatePresence(FikaUIGlobals.EFikaPlayerPresence.IN_HIDEOUT);
        }
    }
}
