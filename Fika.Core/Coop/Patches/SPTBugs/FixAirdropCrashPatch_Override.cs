using Comfort.Common;
using EFT;
using EFT.SynchronizableObjects;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Reflection;

namespace Fika.Core.Coop.Patches.SPTBugs
{
	// This patch fixes a bug in SPT 3.10.0 where ServerAirdropManager being disposed, and this is null for any clients.
	// It will be fixed in SPT 3.10.1, but to keep backwards compatibility keep this patch in until 3.11 for players that refuse to update their SPT version.
	internal class FixAirdropCrashPatch_Override : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(TarkovApplication).GetMethod(nameof(TarkovApplication.method_48));
		}

		[PatchPrefix]
		public static void Prefix()
		{
			if (!Singleton<GameWorld>.Instantiated)
			{
				return;
			}

			GameWorld gameWorld = Singleton<GameWorld>.Instance;
			if (gameWorld.SynchronizableObjectLogicProcessor is null)
			{
				return;
			}

			List<SynchronizableObject> syncObjects = Traverse.Create(gameWorld.SynchronizableObjectLogicProcessor)?.Field<List<SynchronizableObject>>("list_0")?.Value;
			if (syncObjects is null)
			{
				return;
			}

			foreach (SynchronizableObject obj in syncObjects)
			{
				obj.Logic.ReturnToPool();
				obj.ReturnToPool();
			}

			// Without this check can cause black screen when backing out of raid prior to airdrop manager being init
			if (gameWorld.SynchronizableObjectLogicProcessor.AirdropManager is not null)
			{
				if (gameWorld.SynchronizableObjectLogicProcessor is SynchronizableObjectLogicProcessorClass synchronizableObjectLogicProcessorClass)
				{
					synchronizableObjectLogicProcessorClass.ServerAirdropManager?.Dispose();
				}

				gameWorld.SynchronizableObjectLogicProcessor.Dispose();
			}
		}
	}
}
