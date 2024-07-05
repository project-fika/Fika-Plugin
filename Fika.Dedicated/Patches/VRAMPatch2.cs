using System;
using System.Reflection;
using HarmonyLib;
using Aki.Reflection.Patching;
using UnityEngine;

namespace Fika.Headless.Patches
{
    // Token: 0x0200000A RID: 10
    public class VRAMPatch2 : ModulePatch
    {
        // Token: 0x0600001D RID: 29 RVA: 0x00002440 File Offset: 0x00000640
        protected override MethodBase GetTargetMethod()
        {
            return typeof(CameraClass).GetMethod("SetCamera");
        }

        // Token: 0x0600001E RID: 30 RVA: 0x00002468 File Offset: 0x00000668
        [PatchPrefix]
        private static bool Prefix(CameraClass __instance, Camera camera)
        {
            __instance.Reset();
            __instance.Camera = camera;
            __instance.method_2();
            Action action = Traverse.Create(__instance).Field<Action>("action_1").Value;
            if (action != null)
            {
                action();
            }
            return false;
        }
    }
}
