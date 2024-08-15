using Comfort.Common;
using EFT;
using Fika.Core.Coop.Utils;
using SPT.Common.Http;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
using System.Reflection;

namespace Fika.Core.Coop.Patches.Overrides
{
	/// <summary>
	/// Override of SPT patch to reduce data requests to server. <br/> 
	/// <see href="https://dev.sp-tarkov.com/SPT/Modules/src/commit/599b5ec5203a32a9f4adcb7fae810b2103be5de0/project/SPT.SinglePlayer/Patches/RaidFix/MaxBotPatch.cs">Source</see>
	/// </summary>
	/// <returns></returns>
	internal class MaxBotPatch_Override : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
			const string methodName = "SetSettings";
			System.Type desiredType = PatchConstants.EftTypes.SingleCustom(x => x.GetMethod(methodName, flags) != null && IsTargetMethod(x.GetMethod(methodName, flags)));
			MethodInfo desiredMethod = desiredType.GetMethod(methodName, flags);

			Logger.LogDebug($"{GetType().Name} Type: {desiredType?.Name}");
			Logger.LogDebug($"{GetType().Name} Method: {desiredMethod?.Name}");

			return desiredMethod;
		}

		private static bool IsTargetMethod(MethodInfo mi)
		{
			ParameterInfo[] parameters = mi.GetParameters();
			return parameters.Length == 3 && parameters[0].Name == "maxCount" && parameters[1].Name == "botPresets" && parameters[2].Name == "botScatterings";
		}

		[PatchPrefix]
		private static void PatchPreFix(ref int maxCount)
		{
			if (FikaBackendUtils.IsServer)
			{
				GameWorld gameWorld = Singleton<GameWorld>.Instance;
				string location = gameWorld.MainPlayer.Location;

				if (int.TryParse(RequestHandler.GetJson($"/singleplayer/settings/bot/maxCap/{location ?? "default"}"), out int parsedMaxCount))
				{
					Logger.LogWarning($"Set max bot cap to: {parsedMaxCount}");
					maxCount = parsedMaxCount;
				}
				else
				{
					Logger.LogWarning($"Unable to parse data from singleplayer/settings/bot/maxCap, using existing map max of {maxCount}");
				}
			}
			else
			{
				maxCount = 0;
			}
		}
	}
}
