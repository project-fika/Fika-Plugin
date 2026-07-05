namespace Fika.Core.Networking.Packets.Communication;

public struct ClearSnapshotterPacket : INetSerializable
{
    public int NetId;

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
    }

    public void Deserialize(NetDataReader reader)
    {
        NetId = reader.GetInt();
    }
}
