namespace Fika.Core.Networking.Packets.World;

public struct RallyZonePresencePacket : INetSerializable
{
    public int ZoneId;
    public int NetId;
    public bool Entered;

    public void Deserialize(NetDataReader reader)
    {
        ZoneId = reader.GetInt();
        NetId = reader.GetInt();
        Entered = reader.GetBool();
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(ZoneId);
        writer.Put(NetId);
        writer.Put(Entered);
    }
}
