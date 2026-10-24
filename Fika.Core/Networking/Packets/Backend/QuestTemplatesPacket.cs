namespace Fika.Core.Networking.Packets.Backend;

public struct QuestTemplatesPacket : INetSerializable
{
    public int NetId;
    public byte[] Data;

    public void Deserialize(NetDataReader reader)
    {
        NetId = reader.GetInt();
        Data = reader.GetByteArray();
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.PutByteArray(Data);
    }
}
