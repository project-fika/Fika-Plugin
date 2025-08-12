using LiteNetLib.Utils;

namespace Fika.Core.Networking.Packets.Communication;

public struct VOIPPacket : INetSerializable
{
    public byte[] Data;

    public void Deserialize(NetDataReader reader)
    {
        Data = reader.GetByteArray();
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.PutByteArray(Data);
    }
}
