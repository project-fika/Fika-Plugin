using EFT.CameraControl;
using Fika.Core.Patching;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Fika.Core.Coop.Patches.Camera
{
    /// <summary>
    /// This patch removes the camera reset on the <see cref="PlayerCameraController.LateUpdate"/>
    /// </summary>
    public class PlayerCameraController_LateUpdate_Transpiler : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(PlayerCameraController).GetMethod(nameof(PlayerCameraController.LateUpdate));
        }

        [PatchTranspiler]
        public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> newInstructions = [.. instructions];
            for (int i = 11; i < newInstructions.Count; i++)
            {
                newInstructions[i].opcode = OpCodes.Ret;
            }
            return newInstructions;
        }
    }
}
