using Fika.Core.Networking.LiteNetLib.Utils;

namespace Fika.Core.Networking.Packets.Communication;

public struct QuestConditionPacket : INetSerializable
{
    public string Nickname;
    public string Id;
    public string SourceId;

    public void Deserialize(NetDataReader reader)
    {
        Nickname = reader.GetString();
        Id = reader.GetString();
        SourceId = reader.GetString();
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(Nickname);
        writer.Put(Id);
        writer.Put(SourceId);
    }
}
