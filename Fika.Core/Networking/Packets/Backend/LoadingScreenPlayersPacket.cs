namespace Fika.Core.Networking.Packets.Backend;

public struct LoadingScreenPlayersPacket : INetSerializable
{
    public int[] NetIds;
    public string[] Nicknames;

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.PutArray(NetIds);
        writer.PutArray(Nicknames);
    }

    public void Deserialize(NetDataReader reader)
    {
        NetIds = reader.GetArray<int>();
        Nicknames = reader.GetStringArray();
    }
}
