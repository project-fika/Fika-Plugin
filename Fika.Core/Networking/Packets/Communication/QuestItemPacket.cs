using EFT;

namespace Fika.Core.Networking.Packets.Communication;

public struct QuestItemPacket : INetSerializable
{
    public string Nickname;
    public MongoID? ItemId;

    public void Deserialize(NetDataReader reader)
    {
        Nickname = reader.GetString();
        ItemId = reader.GetNullableMongoID();
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(Nickname);
        writer.PutNullableMongoID(ItemId);
    }
}
