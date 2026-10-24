namespace Fika.Core.Networking.Packets.World;

public struct CutsceneEndPacket : INetSerializable
{
    public int ProcessId;
    public bool Skip;

    public void Deserialize(NetDataReader reader)
    {
        ProcessId = reader.GetInt();
        Skip = reader.GetBool();
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(ProcessId);
        writer.Put(Skip);
    }
}
