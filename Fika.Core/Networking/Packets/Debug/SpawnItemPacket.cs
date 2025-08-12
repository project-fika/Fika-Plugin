using EFT.InventoryLogic;
using LiteNetLib.Utils;

namespace Fika.Core.Networking.Packets.Debug;

public struct SpawnItemPacket : INetSerializable
{
    public int NetId;
    public Item Item;

    public void Deserialize(NetDataReader reader)
    {
        NetId = reader.GetInt();
        Item = reader.GetItem();
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.PutItem(Item);
    }
}
