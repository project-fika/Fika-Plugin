using SPT.Reflection.Patching;
using System;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
	/// <summary>
	/// Fixes a live bug where there is no null check on the <see cref="AIPlaceInfo"/> during <see cref="AIPlaceLogicPartisan.Dispose"/>, causing a<see cref="NullReferenceException"/> if it is null
	/// </summary>
	public class AIPlaceLogicPartisan_Dispose_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(AIPlaceLogicPartisan).GetMethod(nameof(AIPlaceLogicPartisan.Dispose));
		}

		[PatchPrefix]
		public static bool Prefix(AIPlaceInfo ___aiplaceInfo_0)
		{
			if (___aiplaceInfo_0 == null)
			{
				return false;
			}

			return true;
		}
	}
}
