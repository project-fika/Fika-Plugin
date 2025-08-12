namespace Fika.Core.Networking.Packets.World;

public class WorldLootPacket : INetSerializable
{
    public byte[] Data;

    public void Deserialize(NetDataReader reader)
    {
        Data = reader.DecompressAndGetByteArray();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.CompressAndPutByteArray(Data);
    }
}
