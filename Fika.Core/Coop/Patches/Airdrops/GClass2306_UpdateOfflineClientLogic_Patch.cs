using Comfort.Common;
using Fika.Core.Networking;
using LiteNetLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
	public class GClass2335_UpdateOfflineClientLogic_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(GClass2335).GetMethod(nameof(GClass2335.UpdateOfflineClientLogic));
		}

		[PatchPostfix]
		public static void Postfix(AirplaneDataPacketStruct ___airplaneDataPacketStruct)
		{
			SyncObjectPacket packet = new(___airplaneDataPacketStruct.ObjectId)
			{
				ObjectType = EFT.SynchronizableObjects.SynchronizableObjectType.AirDrop,
				Data = ___airplaneDataPacketStruct
			};

			Singleton<FikaServer>.Instance.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
		}
	}
}
