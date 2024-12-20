using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Fika.Core.Coop.Patches
{
	/// <summary>
	/// This patch stops BSGs dogtag handling as it is poorly executed
	/// </summary>
	public class Player_method_144_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			//Check for gclass increments
			return typeof(LocalPlayer).GetMethod(nameof(LocalPlayer.method_144));
		}

		[PatchPrefix]
		public static bool Prefix()
		{
			return false;
		}
	}
}
