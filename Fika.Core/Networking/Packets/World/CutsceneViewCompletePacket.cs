namespace Fika.Core.Networking.Packets.World;

public struct CutsceneViewCompletePacket : INetSerializable
{
    public int NetId;
    public int ProcessId;

    public void Deserialize(NetDataReader reader)
    {
        NetId = reader.GetInt();
        ProcessId = reader.GetInt();
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.Put(ProcessId);
    }
}
