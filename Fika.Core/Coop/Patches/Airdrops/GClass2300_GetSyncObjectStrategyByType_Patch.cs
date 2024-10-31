using EFT.SynchronizableObjects;
using Fika.Core.Coop.Utils;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
	public class GClass2400_GetSyncObjectStrategyByType_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(GClass2400).GetMethod(nameof(GClass2400.GetSyncObjectStrategyByType), BindingFlags.Static | BindingFlags.Public);
		}

		[PatchPrefix]
		public static bool Prefix(SynchronizableObjectType type, ref ISynchronizableObject __result)
		{
			switch (type)
			{
				case SynchronizableObjectType.Tripwire:
					__result = new GClass2399();
					break;
				case SynchronizableObjectType.AirPlane:
					__result = new AirplaneLogicClass(FikaBackendUtils.IsServer);
					break;
				case SynchronizableObjectType.AirDrop:
					__result = new AirdropLogicClass(FikaBackendUtils.IsServer);
					break;
				default:
					break;
			}

			return false;
		}
	}
}
