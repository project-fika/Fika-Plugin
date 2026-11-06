namespace Fika.Core.Networking.Packets.World;

public struct RallyZoneUpdatePacket : INetSerializable
{
    public int ZoneId;
    public byte PlayersInZone;
    public byte TotalAlive;

    public void Deserialize(NetDataReader reader)
    {
        ZoneId = reader.GetInt();
        PlayersInZone = reader.GetByte();
        TotalAlive = reader.GetByte();
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(ZoneId);
        writer.Put(PlayersInZone);
        writer.Put(TotalAlive);
    }
}
