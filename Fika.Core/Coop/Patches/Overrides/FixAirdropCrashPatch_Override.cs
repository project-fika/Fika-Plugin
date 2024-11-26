using Comfort.Common;
using EFT;
using EFT.SynchronizableObjects;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Reflection;

namespace Fika.Core.Coop.Patches.Overrides
{
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
