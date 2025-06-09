using EFT.RocketLauncher;
using Fika.Core.Patching;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Fika.Core.Coop.Patches.Rockets
{
    /// <summary>
    /// Adds a check to only run coneblast if the rocket is local
    /// </summary>
    public class RocketProjectile_Launch_Transpiler : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(RocketProjectile)
                .GetMethod(nameof(RocketProjectile.Launch));
        }

        [PatchTranspiler]
        public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> instructionsList = [.. instructions];

            Label label1 = generator.DefineLabel();

            instructionsList[16].labels.Add(label1);

            FieldInfo backblastField = typeof(RocketProjectile)
                .GetField("gclass3734_0", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo playerField = typeof(GClass3734)
                .GetField(nameof(GClass3734.Iplayer_0));

            instructionsList[2] = new(OpCodes.Brfalse_S, label1);

            CodeInstruction instruction = new(OpCodes.Ldarg_0);
            CodeInstruction instruction2 = new(OpCodes.Ldfld, backblastField);
            CodeInstruction instruction3 = new(OpCodes.Ldfld, playerField);
            CodeInstruction instruction4 = new(OpCodes.Brfalse_S, label1);

            instructionsList.Insert(3, instruction);
            instructionsList.Insert(4, instruction2);
            instructionsList.Insert(5, instruction3);
            instructionsList.Insert(6, instruction4);

            return instructionsList;
        }
    }
}
