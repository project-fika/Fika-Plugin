using Comfort.Common;
using Fika.Core.Coop.GameMode;
using Fika.Core.Networking;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
	/// <summary>
	/// Used to help us keep track of thrown grenades during a session for kill progression
	/// </summary>
	public class GrenadeClass_Init_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(GrenadeFactoryClass).GetMethod(nameof(GrenadeFactoryClass.Create));
		}

		[PatchPostfix]
		public static void Postfix(ThrowWeapItemClass item)
		{
			CoopGame coopGame = Singleton<IFikaNetworkManager>.Instance.CoopHandler.LocalGameInstance;
			if (coopGame != null)
			{
				coopGame.ThrownGrenades.Add(item);
			}
		}
	}
}
