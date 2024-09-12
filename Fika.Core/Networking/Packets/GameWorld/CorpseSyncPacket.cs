using EFT.InventoryLogic;
using LiteNetLib.Utils;
using System.Security.Policy;

namespace Fika.Core.Networking.Packets.GameWorld
{
	public struct CorpseSyncPacket(int netId) : INetSerializable
	{
		public int NetId = netId;
		public InventoryEquipment Equipment;
		public string ItemInHandsId;

		public void Deserialize(NetDataReader reader)
		{
			NetId = reader.GetInt();
			Equipment = (InventoryEquipment)reader.GetItem();
			ItemInHandsId = reader.GetString();
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(NetId);
			writer.PutItem(Equipment);
			writer.Put(ItemInHandsId);
		}
	}
}
