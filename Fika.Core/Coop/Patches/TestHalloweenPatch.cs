using EFT;
using SPT.Reflection.Patching;
using System.Reflection;
using UnityEngine;

namespace Fika.Core.Coop.Patches
{
	public class TestHalloweenPatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(HalloweenEventVisual).GetMethod(nameof(HalloweenEventVisual.Initialize));
		}

		[PatchPrefix]
		public static void Prefix(HalloweenEventVisual __instance, bool ___bool_0, HalloweenVisualContainer ____container, Vector3[] positions)
		{
			if (__instance == null)
			{
				FikaPlugin.Instance.FikaLogger.LogError("INSTANCE WAS NULL");
				return;
			}

			if (____container == null)
			{
				FikaPlugin.Instance.FikaLogger.LogError("CONTAINER WAS NULL");
				return;
			}

			if (positions == null)
			{
				FikaPlugin.Instance.FikaLogger.LogError("POSITIONS WAS NULL");
				return;
			}

			FikaPlugin.Instance.FikaLogger.LogWarning($"Halloween Test Patch: transform: {__instance.transform + " " + __instance.transform.name}, bool: {___bool_0}, container: {____container}, positions: {positions}; {positions.Length}; {positions[0].ToStringHighResolution()}");
		}
	}
}
