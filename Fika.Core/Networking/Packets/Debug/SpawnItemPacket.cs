using EFT.InventoryLogic;
using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
	public struct SpawnItemPacket : INetSerializable
	{
		public int NetId;
		public Item Item;

		public void Deserialize(NetDataReader reader)
		{
			NetId = reader.GetInt();
			Item = reader.GetItem();
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(NetId);
			writer.PutItem(Item);
		}
	}
}
