namespace Fika.Core.Networking.Packets.World;

public struct CutsceneSkipPacket : INetSerializable
{
    public int ProcessId;
    public bool Skip;
    public int PlayersCount;
    public int[] SkippingNetIds;

    public void Deserialize(NetDataReader reader)
    {
        ProcessId = reader.GetInt();
        Skip = reader.GetBool();
        if (Skip)
        {
            return;
        }

        PlayersCount = reader.GetInt();
        SkippingNetIds = reader.GetIntArray();
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(ProcessId);
        writer.Put(Skip);
        if (Skip)
        {
            return;
        }

        writer.Put(PlayersCount);
        writer.PutArray(SkippingNetIds);
    }
}
