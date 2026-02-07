using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.Player.Common.SubPackets;

public sealed class DropPacket : IPoolSubPacket
{
    private DropPacket()
    {

    }

    public static DropPacket CreateInstance()
    {
        return new();
    }

    public static DropPacket FromValue(bool fastDrop)
    {
        var packet = CommonSubPacketPoolManager.Instance.GetPacket<DropPacket>(ECommonSubPacketType.Drop);
        packet.FastDrop = fastDrop;
        return packet;
    }

    public bool FastDrop;

    public void Execute(FikaPlayer player)
    {
        player.HandleDropPacket(FastDrop);
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(FastDrop);
    }

    public void Deserialize(NetDataReader reader)
    {
        FastDrop = reader.GetBool();
    }

    public void Dispose()
    {
        FastDrop = false;
    }
}
