using EFT.UI;
using System.Linq;
using System.Reflection;
using EFT;
using Fika.Core.Main.Components;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.Revival;

[IgnoreAutoPatch]
internal sealed class GetActionsClass_GetAvailableActions_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(InteractionContextHelper)
            .GetMethods()
            .First(x => x.Name == nameof(InteractionContextHelper.GetAvailableActions)
                && x.GetParameters()[0].ParameterType.Equals(typeof(GamePlayerOwner)));
    }

    [PatchPrefix]
    public static bool Prefix(GamePlayerOwner owner, IInteractive interactive, ref AvailableInteractionState __result)
    {
        if (interactive is not ReviveInteractable reviveInteractable)
        {
            return true;
        }

        __result = reviveInteractable.GetActions(owner);
        return false;
    }
}
