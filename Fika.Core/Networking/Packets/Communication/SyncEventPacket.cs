namespace Fika.Core.Networking.Packets.Communication;

public struct SyncEventPacket : INetSerializable
{
    public int Type;
    public byte[] Data;

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(Type);
        writer.PutByteArray(Data);
    }

    public void Deserialize(NetDataReader reader)
    {
        Type = reader.GetInt();
        Data = reader.GetByteArray();
    }
}
