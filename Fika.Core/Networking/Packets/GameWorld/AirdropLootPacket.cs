// © 2024 Lacyway All Rights Reserved

using EFT.InventoryLogic;
using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
	public struct AirdropLootPacket(bool isRequest = false) : INetSerializable
	{
		public bool IsRequest = isRequest;
		public string ContainerId;
		public int ContainerNetId;
		public Item RootItem;

		public void Deserialize(NetDataReader reader)
		{
			IsRequest = reader.GetBool();
			ContainerId = reader.GetString();
			ContainerNetId = reader.GetInt();
			RootItem = reader.GetAirdropItem();
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(IsRequest);
			writer.Put(ContainerId);
			writer.Put(ContainerNetId);
			writer.PutAirdropItem(RootItem);
		}
	}
}
