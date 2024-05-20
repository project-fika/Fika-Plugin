// © 2024 Lacyway All Rights Reserved

using Aki.Reflection.Patching;
using EFT.Weather;
using Fika.Core.Coop.Matchmaker;
using System.Reflection;

namespace Fika.Core.Coop.World
{
    internal class WeatherNode_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(WeatherController).GetMethod(nameof(WeatherController.method_4));

        [PatchPostfix]
        public static void Postfix(WeatherController __instance, WeatherClass[] nodes)
        {
            if (MatchmakerAcceptPatches.IsClient)
            {
                return;
            }

            MatchmakerAcceptPatches.Nodes = nodes;
        }
    }
}
