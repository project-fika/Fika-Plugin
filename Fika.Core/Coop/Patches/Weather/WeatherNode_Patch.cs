// © 2024 Lacyway All Rights Reserved

using EFT.Weather;
using Fika.Core.Coop.Utils;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches.Weather
{
	internal class WeatherNode_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod() => typeof(WeatherController).GetMethod(nameof(WeatherController.method_0));

		[PatchPostfix]
		public static void Postfix(WeatherController __instance, WeatherClass[] nodes)
		{
			if (FikaBackendUtils.IsClient)
			{
				return;
			}

			FikaBackendUtils.Nodes = nodes;
		}
	}
}
