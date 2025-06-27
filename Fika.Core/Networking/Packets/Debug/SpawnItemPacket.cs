using EFT.InventoryLogic;
using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
    public struct SpawnItemPacket : INetSerializable
    {
        public ushort NetId;
        public Item Item;

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetUShort();
            Item = reader.GetItem();
        }

        public readonly void Serialize(NetDataWriter writer)
        {
            writer.Put(NetId);
            writer.PutItem(Item);
        }
    }
}
