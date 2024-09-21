using EFT;
using EFT.Interactive;
using Fika.Core.Coop.Utils;
using SPT.Reflection.Patching;
using System;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
	internal class BufferZoneControllerClass_method_1_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(BufferZoneControllerClass).GetMethod(nameof(BufferZoneControllerClass.method_1));
		}

		[PatchPrefix]
		public static bool Prefix(EGameType gameType, BufferZoneControllerClass __instance, ref bool ___bool_1, ref Action ___action_0)
		{
			AbstractGame.OnGameTypeSetted -= __instance.method_1;

			___bool_1 = gameType == EGameType.Offline;

			if (FikaBackendUtils.IsClient)
			{
				___bool_1 = false;
			}

			if (___bool_1)
			{
				Player.OnPlayerDeadStatic += __instance.method_2;
				LighthouseTraderZone.OnPlayerAllowStatusChanged += __instance.method_4;
			}

			// Fire OnInitialized
			___action_0?.Invoke();

			// Skip the original method
			return false;
		}
	}
}
