namespace Fika.Core.Networking.Packets.Backend;

public struct LoadingScreenPacket : INetSerializable
{
    public int NetId;
    public float Progress;

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.Put(Progress);
    }

    public void Deserialize(NetDataReader reader)
    {
        NetId = reader.GetInt();
        Progress = reader.GetFloat();
    }
}
