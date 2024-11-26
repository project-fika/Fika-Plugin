using EFT;
using Fika.Core.Coop.ObservedClasses;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
	public class BodyPartCollider_SetUpPlayer_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(BodyPartCollider).GetMethod(nameof(BodyPartCollider.SetUpPlayer));
		}

		[PatchPrefix]
		public static bool Prefix(BodyPartCollider __instance, IPlayer iPlayer)
		{
			if (iPlayer != null)
			{
				if (iPlayer is CoopBot coopBot)
				{
					__instance.InitColliderSettings();
					__instance.playerBridge = new BotPlayerBridge(coopBot);
					return false;
				}

				if (iPlayer is ObservedCoopPlayer observedCoopPlayer)
				{
					__instance.InitColliderSettings();
					if (FikaBackendUtils.IsServer)
					{
						__instance.playerBridge = new ObservedHostBridge(observedCoopPlayer);
						return false;
					}
					__instance.playerBridge = new ObservedClientBridge(observedCoopPlayer);
					return false;
				}
			}
			return true;
		}
	}
}
