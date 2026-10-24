using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.RocketLauncher;
using EFT.UI;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.Utils;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.Main.Patches.AbstractGamePatches;

public static class AbstractGame_InRaid_Inlined_Patches
{
    /// <summary>
    /// Check whether the running game is a Fika raid, which <see cref="AbstractGame_InRaid_Patch"/> reports as in raid
    /// </summary>
    /// <returns>True for a <see cref="CoopGame"/> outside the tutorial</returns>
    public static bool InCoopRaid()
    {
        return !FikaBackendUtils.IsTutorial && Singleton<AbstractGame>.Instantiated && Singleton<AbstractGame>.Instance is CoopGame;
    }

    public class Skill_IsInRaid_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.PropertyGetter(typeof(Skill), nameof(Skill.IsInRaid));
        }

        [PatchPostfix]
        public static void Postfix(ref bool __result)
        {
            if (!__result && InCoopRaid())
            {
                __result = true;
            }
        }
    }

    /// <summary>
    /// Armor plates cannot be moved while the armor is equipped in raid
    /// </summary>
    public class ArmorSlot_CanAcceptRaid_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ArmorSlot), nameof(ArmorSlot.CanAcceptRaid));
        }

        [PatchPostfix]
        public static void Postfix(ArmorSlot __instance, ref InventoryError error, ref bool __result)
        {
            if (!__result || !InCoopRaid())
            {
                return;
            }

            var parentItem = __instance.ParentItem;
            var owner = parentItem?.Owner == null ? null : ((Il2CppObjectBase)parentItem.Owner).TryCast<InventoryController>();
            if (owner != null && InventoryExtension.IsItemEquipped(owner, parentItem))
            {
                error = new Slot.EquippedLockedSlot();
                __result = false;
            }
        }
    }

    /// <summary>
    /// In raid the UI routing depends on the open inventory screen
    /// </summary>
    public class GUISounds_UseStandardUIRouting_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GUISounds), nameof(GUISounds.UseStandardUIRouting));
        }

        [PatchPostfix]
        public static void Postfix(ref bool __result)
        {
            if (!InCoopRaid())
            {
                return;
            }

            var context = ItemUiContext.Instance;
            __result = context != null && context.ContextType == EItemUiContextType.ScavengerInventoryScreen;
        }
    }

    /// <summary>
    /// Releases the explosion renderer and its subscriptions, which only happens in raid
    /// </summary>
    public class RocketProjectile_OnDestroy_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(RocketProjectile), nameof(RocketProjectile.OnDestroy));
        }

        [PatchPostfix]
        public static void Postfix(RocketProjectile __instance)
        {
            if (!InCoopRaid())
            {
                return;
            }

            var explosionModel = __instance._explosionModel;
            if (explosionModel != null)
            {
                // The model belongs to this projectile alone, so its only subscriber is the one being destroyed
                explosionModel.OnExplosionCompleatedEventField = null;
                explosionModel.Dispose();
            }

            if (__instance._coneBlastCoroutine != null)
            {
                __instance.StopCoroutine(__instance._coneBlastCoroutine);
                __instance._coneBlastCoroutine = null;
            }
        }
    }

    /// <summary>
    /// In raid the task list sorts by the current location first
    /// </summary>
    public class TasksPanel_Init_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TasksPanel), nameof(TasksPanel.Init));
        }

        [PatchPostfix]
        public static void Postfix(TasksPanel __instance)
        {
            if (__instance._isInRaid || !InCoopRaid())
            {
                return;
            }

            __instance._isInRaid = true;
            __instance._currentLocationId = Singleton<AbstractGame>.Instance.LocationObjectId;
            __instance._questLocationComparer = new QuestLocationComparer(__instance._currentLocationId, __instance._locations, false);
            __instance._sortType = EQuestsSortType.Location;
            __instance._sortAscend = false;
        }
    }
}
