using EFT.CameraControl;
using SPT.Reflection.Patching;
using System.Reflection;
using UnityEngine;

namespace Fika.Core.Coop.Patches.Camera
{
	public class OpticRetrice_UpdateTransform_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(OpticRetrice).GetMethod(nameof(OpticRetrice.UpdateTransform));
		}

		[PatchPrefix]
		public static bool Prefix(OpticSight opticSight, SkinnedMeshRenderer ____renderer)
		{
			return opticSight.ScopeData != null && opticSight.ScopeData.Reticle != null && ____renderer != null;
		}
	}
}
