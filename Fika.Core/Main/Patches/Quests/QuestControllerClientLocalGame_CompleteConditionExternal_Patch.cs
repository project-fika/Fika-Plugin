using System.Reflection;
using EFT.Quests;
using Fika.Core.Main.ObservedClasses;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.Main.Patches.Quests;

public class QuestControllerClientLocalGame_CompleteConditionExternal_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(QuestControllerClientLocalGame).GetMethod("EFT.Quests.IActiveQuestController.CompleteConditionExternal",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
    }

    [PatchPrefix]
    public static bool Prefix(QuestControllerClientLocalGame __instance)
    {
        return __instance.TryCast<ObservedQuestController>() == null;
    }
}
