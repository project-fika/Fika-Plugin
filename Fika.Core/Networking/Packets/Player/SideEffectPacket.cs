using EFT;

namespace Fika.Core.Networking.Packets.Player;

public struct SideEffectPacket : INetSerializable
{
    public MongoID ItemId;
    public float Value;

    public void Deserialize(NetDataReader reader)
    {
        ItemId = reader.GetMongoID();
        Value = reader.GetFloat();
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.PutMongoID(ItemId);
        writer.Put(Value);
    }
}
