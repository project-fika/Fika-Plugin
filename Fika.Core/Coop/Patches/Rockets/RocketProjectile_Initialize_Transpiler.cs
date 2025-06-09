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
    /// Adds a check to only run Backblast checks if <see cref="IPlayer.IsYourPlayer"/>
    /// </summary>
    public class RocketProjectile_Initialize_Transpiler : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(RocketProjectile)
                .GetMethod(nameof(RocketProjectile.Initialize));
        }

        [PatchTranspiler]
        public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> instructionsList = [.. instructions];

            Label label1 = generator.DefineLabel();
            Label label2 = generator.DefineLabel();

            instructionsList[72].labels.Add(label1);
            instructionsList[78].labels.Add(label2);

            MethodInfo method = typeof(IPlayer)
                .GetProperty(nameof(IPlayer.IsYourPlayer))
                .GetGetMethod();

            CodeInstruction instruction = new(OpCodes.Ldarg_2);
            CodeInstruction instruction2 = new(OpCodes.Callvirt, method);
            CodeInstruction instruction3 = new(OpCodes.Brfalse_S, label1);

            instructionsList.Insert(57, instruction);
            instructionsList.Insert(58, instruction2);
            instructionsList.Insert(59, instruction3);

            instructionsList[56] = new(OpCodes.Brfalse_S, label1);
            instructionsList[74] = new(OpCodes.Brtrue_S, label2);

            return instructionsList;
        }
    }
}
