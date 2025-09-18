using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using System.Threading.Tasks;

namespace Fika.Core.Main.Patches.DebugPatches;

public static class TasksExtensions_HandleFinishedTask_Patches
{
    [DebugPatch]
    internal class TasksExtensions_HandleFinishedTask_Patch1 : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TasksExtensions), nameof(TasksExtensions.HandleFinishedTask), [typeof(Task)]);
        }

        [PatchPrefix]
        public static bool Prefix(Task task)
        {
            if (task.IsFaulted)
            {
                Logger.LogError($"TasksExtensions_HandleFinishedTask_Patch1: {task.Exception}");
            }

            return true;
        }
    }

    [DebugPatch]
    internal class TasksExtensions_HandleFinishedTask_Patch2 : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TasksExtensions), nameof(TasksExtensions.HandleFinishedTask), [typeof(Task), typeof(object)]);
        }

        [PatchPrefix]
        public static bool Prefix(Task task, object errorMessage)
        {
            if (task.IsFaulted)
            {
                Logger.LogError($"TasksExtensions_HandleFinishedTask_Patch2: {task.Exception}");
            }

            return true;
        }
    }
}