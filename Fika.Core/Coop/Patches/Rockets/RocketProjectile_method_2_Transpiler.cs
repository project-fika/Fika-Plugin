using EFT;
using EFT.RocketLauncher;
using Fika.Core.Patching;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Fika.Core.Coop.Patches.Rockets
{
    /// <summary>
    /// If <see cref="IPlayer"/> is null, the rocket was not created locally. Return early.
    /// </summary>
    public class RocketProjectile_method_2_Transpiler : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(RocketProjectile)
                .GetMethod(nameof(RocketProjectile.method_6));
        }

        [PatchTranspiler]
        public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> instructionsList = [.. instructions];

            Label label1 = generator.DefineLabel();

            FieldInfo backblastField = typeof(RocketProjectile)
                .GetField("gclass3734_0", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo playerField = typeof(GClass3734)
                .GetField(nameof(GClass3734.Iplayer_0));

            CodeInstruction instruction = new(OpCodes.Ldarg_0);
            CodeInstruction instruction2 = new(OpCodes.Ldfld, backblastField);
            CodeInstruction instruction3 = new(OpCodes.Ldfld, playerField);
            CodeInstruction instruction4 = new(OpCodes.Brtrue_S, label1);
            CodeInstruction instruction5 = new(OpCodes.Ret);

            instructionsList.Insert(0, instruction);
            instructionsList.Insert(1, instruction2);
            instructionsList.Insert(2, instruction3);
            instructionsList.Insert(3, instruction4);
            instructionsList.Insert(4, instruction5);

            instructionsList[5].labels.Add(label1);

            return instructionsList;
        }
    }
}
