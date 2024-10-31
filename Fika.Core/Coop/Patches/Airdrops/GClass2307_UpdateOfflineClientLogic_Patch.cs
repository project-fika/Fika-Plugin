using Comfort.Common;
using Fika.Core.Networking;
using LiteNetLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
	public class GClass2407_UpdateOfflineClientLogic_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(GClass2407).GetMethod(nameof(GClass2407.UpdateOfflineClientLogic));
		}

		[PatchPostfix]
		public static void Postfix(AirplaneDataPacketStruct ___airplaneDataPacketStruct)
		{
			SyncObjectPacket packet = new(___airplaneDataPacketStruct.ObjectId)
			{
				ObjectType = EFT.SynchronizableObjects.SynchronizableObjectType.AirPlane,
				Data = ___airplaneDataPacketStruct
			};

			Singleton<FikaServer>.Instance.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
		}
	}
}
