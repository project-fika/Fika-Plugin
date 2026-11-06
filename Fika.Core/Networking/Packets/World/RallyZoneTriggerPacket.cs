namespace Fika.Core.Networking.Packets.World;

public struct RallyZoneTriggerPacket : INetSerializable
{
    public string TriggerId;
    public int OriginNetId;

    public void Deserialize(NetDataReader reader)
    {
        TriggerId = reader.GetString();
        OriginNetId = reader.GetInt();
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(TriggerId);
        writer.Put(OriginNetId);
    }
}
