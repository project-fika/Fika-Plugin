using LiteNetLib.Utils;
using static Fika.Core.Networking.Packets.SubPackets;

namespace Fika.Core.Networking
{
	public struct InventoryPacket(int netId) : INetSerializable
	{
		public int NetId = netId;
		public bool HasItemControllerExecutePacket = false;
		public ItemControllerExecutePacket ItemControllerExecutePacket;

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(NetId);
			writer.Put(HasItemControllerExecutePacket);
			if (HasItemControllerExecutePacket)
			{
				writer.PutItemControllerExecutePacket(ItemControllerExecutePacket);
			}
		}

		public void Deserialize(NetDataReader reader)
		{
			NetId = reader.GetInt();
			HasItemControllerExecutePacket = reader.GetBool();
			if (HasItemControllerExecutePacket)
			{
				ItemControllerExecutePacket = reader.GetItemControllerExecutePacket();
			}
		}
	}
}
