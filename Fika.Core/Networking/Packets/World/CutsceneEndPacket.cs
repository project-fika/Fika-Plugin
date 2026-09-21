namespace Fika.Core.Networking.Packets.World;

public struct CutsceneEndPacket : INetSerializable
{
    public int ProcessId;

    public void Deserialize(NetDataReader reader)
    {
        ProcessId = reader.GetInt();
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(ProcessId);
    }
}
