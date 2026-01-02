#if DEBUG
using System.Reflection;
using EFT;
using Fika.Core.Main.Utils;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.Testing;

public class TestPatchRundans : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(RunddansControllerClass),
            nameof(RunddansControllerClass.GetAvailableInteraction));
    }

    [PatchPrefix]
    public static void Prefix(GamePlayerOwner owner, int objectId, RunddansControllerClass __instance)
    {
        if (!__instance.IsValid(owner.Player, out TransitInteractionControllerAbstractClass transitInteractionControllerAbstractClass, out var transitDataClass))
        {
            FikaGlobals.LogError($"Could not find transit data");
        }

        if (!transitDataClass.events)
        {
            FikaGlobals.LogError("Was not events");
        }

        if (!__instance.Objects.TryGetValue(objectId, out var eventObject))
        {
            FikaGlobals.LogError($"EventObject with id {objectId} not found");
        }
    }
}
#endif