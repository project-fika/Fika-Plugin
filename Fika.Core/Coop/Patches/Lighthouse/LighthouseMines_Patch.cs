using Comfort.Common;
using EFT;
using Fika.Core.Coop.Lighthouse;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using UnityEngine;

namespace Fika.Core.Coop.Patches.Lighthouse
{
	public class LighthouseMines_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return AccessTools.Method(typeof(MineDirectional), nameof(MineDirectional.method_1));
		}

		[PatchPostfix]
		private static void PatchPostfix(Collider other, ref bool __result, MineDirectional __instance)
		{
			GameWorld gameWorld = Singleton<GameWorld>.Instance;
			FikaLighthouseProgressionClass LighthouseProgressionClass = gameWorld.GetComponent<FikaLighthouseProgressionClass>();

			if (gameWorld == null || LighthouseProgressionClass == null)
			{
				return;
			}

			if (gameWorld.MainPlayer.Location.ToLower() != "lighthouse" || __instance.transform.parent.gameObject.name != "Directional_mines_LHZONE")
			{
				return;
			}

			if (FikaBackendUtils.IsServer)
			{
				CoopPlayer player = (CoopPlayer)gameWorld.GetPlayerByCollider(other);

				if (LighthouseProgressionClass.PlayersWithDSP.Contains(player))
				{
					//Do not make player go kaboom if he has a transmitter in this area.
					__result = true;
				}
			}
			else
			{
				if (LighthouseProgressionClass.Transmitter != null)
				{
					//Do not make player go kaboom if he has a transmitter in this area.
					__result = true;
				}
			}
		}
	}
}
