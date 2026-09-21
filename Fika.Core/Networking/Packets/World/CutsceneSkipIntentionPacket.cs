namespace Fika.Core.Networking.Packets.World;

public struct CutsceneSkipIntentionPacket : INetSerializable
{
    public int NetId;
    public int ProcessId;
    public string CutsceneId;
    public bool WantsToSkip;

    public void Deserialize(NetDataReader reader)
    {
        NetId = reader.GetInt();
        ProcessId = reader.GetInt();
        CutsceneId = reader.GetString();
        WantsToSkip = reader.GetBool();
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.Put(ProcessId);
        writer.Put(CutsceneId);
        writer.Put(WantsToSkip);
    }
}
