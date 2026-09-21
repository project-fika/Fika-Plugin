using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.Main.Patches.DebugPatches;

public static class TasksExtensions_HandleFinishedTask_Patches
{
    [DebugPatch]
    internal class TasksExtensions_HandleFinishedTask_Patch1 : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TasksExtensions), nameof(TasksExtensions.HandleFinishedTask), [typeof(Il2CppSystem.Threading.Tasks.Task)]);
        }

        [PatchPrefix]
        public static bool Prefix(Il2CppSystem.Threading.Tasks.Task task)
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
            return AccessTools.Method(typeof(TasksExtensions), nameof(TasksExtensions.HandleFinishedTask), [typeof(Il2CppSystem.Threading.Tasks.Task), typeof(Il2CppSystem.Object)]);
        }

        [PatchPrefix]
        public static bool Prefix(Il2CppSystem.Threading.Tasks.Task task, Il2CppSystem.Object errorMessage)
        {
            if (task.IsFaulted)
            {
                Logger.LogError($"TasksExtensions_HandleFinishedTask_Patch2: {task.Exception}");
            }

            return true;
        }
    }
}