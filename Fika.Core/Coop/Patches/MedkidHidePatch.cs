using EFT;
using Fika.Core.Coop.Utils;
using SPT.Reflection.Patching;
using System;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
	internal class MedkidHidePatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(Player.MedsController).GetMethod(nameof(Player.MedsController.Class1158.HideWeapon));
		}

		[PatchPrefix]
		public static void Prefix()
		{
			if (FikaBackendUtils.IsClient)
			{
				Logger.LogWarning("Player.MedsController.Class1158.HideWeapon ran but it shouldn't!\n" + Environment.StackTrace);
			}
		}
	}
}
