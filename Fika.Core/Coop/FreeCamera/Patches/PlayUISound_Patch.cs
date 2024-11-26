using EFT.UI;
using Fika.Core.Coop.Utils;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.FreeCamera.Patches
{
	public class PlayUISound_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(GUISounds).GetMethod(nameof(GUISounds.PlayUISound), [typeof(EUISoundType)]);
		}

		[PatchPrefix]
		private static bool Prefix(ref EUISoundType soundType)
		{
			if (soundType == EUISoundType.PlayerIsDead)
			{
				// Don't play player dead sound if spectator mode
				if (FikaBackendUtils.IsSpectator)
				{
					return false;
				}
			}

			return true;
		}
	}
}