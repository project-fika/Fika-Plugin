using Comfort.Common;
using EFT;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
	public class GClass3299_ExceptAI_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(GClass3299).GetMethod(nameof(GClass3299.ExceptAI));
		}

		[PatchPrefix]
		public static bool Prefix(IEnumerable<IPlayer> persons, ref IEnumerable<IPlayer> __result)
		{
			if (persons != null)
			{
				if (FikaBackendUtils.IsDedicated)
				{
					List<IPlayer> humanPlayers = new(Singleton<IFikaNetworkManager>.Instance.CoopHandler.HumanPlayers);
					humanPlayers.Remove(Singleton<GameWorld>.Instance.MainPlayer);
					__result = humanPlayers;
					return false;
				}

				__result = Singleton<IFikaNetworkManager>.Instance.CoopHandler.HumanPlayers;
				return false;
			}

			return true;
		}
	}
}
