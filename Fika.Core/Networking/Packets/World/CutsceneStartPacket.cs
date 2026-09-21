namespace Fika.Core.Networking.Packets.World;

public struct CutsceneStartPacket : INetSerializable
{
    public int ProcessId;
    public string CutsceneId;
    public float InitTime;
    public bool FixedTime;
    public bool ForceMode;
    public int[] NetIds;

    public void Deserialize(NetDataReader reader)
    {
        ProcessId = reader.GetInt();
        CutsceneId = reader.GetString();
        InitTime = reader.GetFloat();
        FixedTime = reader.GetBool();
        ForceMode = reader.GetBool();
        NetIds = reader.GetIntArray();
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(ProcessId);
        writer.Put(CutsceneId);
        writer.Put(InitTime);
        writer.Put(FixedTime);
        writer.Put(ForceMode);
        writer.PutArray(NetIds);
    }
}
