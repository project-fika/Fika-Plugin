using System.Linq;
using System.Reflection;
using EFT;
using Fika.Core.Main.Components;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.Revival;

internal sealed class GetActionsClass_GetAvailableActions_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(GetActionsClass)
            .GetMethods()
            .First(x => x.Name == nameof(GetActionsClass.GetAvailableActions)
                && x.GetParameters()[0].ParameterType.Equals(typeof(GamePlayerOwner)));
    }

    [PatchPrefix]
    public static bool Prefix(GamePlayerOwner owner, GInterface177 interactive, ref ActionsReturnClass __result)
    {
        if (interactive is not ReviveInteractable reviveInteractable)
        {
            return true;
        }

        __result = reviveInteractable.GetActions(owner);
        return false;
    }
}
