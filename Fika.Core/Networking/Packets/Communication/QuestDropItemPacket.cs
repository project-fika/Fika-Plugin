using EFT;

namespace Fika.Core.Networking.Packets.Communication;

public struct QuestDropItemPacket(string nickname, string itemId, string zoneId) : INetSerializable
{
    public string Nickname = nickname;
    public MongoID? ItemId = itemId;
    public string ZoneId = zoneId;

    public void Deserialize(NetDataReader reader)
    {
        Nickname = reader.GetString();
        ItemId = reader.GetNullableMongoID();
        ZoneId = reader.GetString();
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(Nickname);
        writer.PutNullableMongoID(ItemId);
        writer.Put(ZoneId);
    }
}
