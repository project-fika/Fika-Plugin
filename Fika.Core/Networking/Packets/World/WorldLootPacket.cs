namespace Fika.Core.Networking.Packets.World;

public class WorldLootPacket : INetSerializable
{
    public string LocationId;
    public byte[] Data;

    public void Deserialize(NetDataReader reader)
    {
        LocationId = reader.GetString();
        Data = reader.DecompressAndGetByteArray();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(LocationId ?? string.Empty);
        writer.CompressAndPutByteArray(Data);
    }
}
