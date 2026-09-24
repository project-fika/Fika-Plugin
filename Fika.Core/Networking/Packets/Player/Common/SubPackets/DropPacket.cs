using Fika.Core.Main.Players;

namespace Fika.Core.Networking.Packets.Player.Common.SubPackets;

public readonly struct DropPacket
{
    public DropPacket(bool fastDrop)
    {
        FastDrop = fastDrop;
    }

    public readonly bool FastDrop;

    public readonly void Execute(FikaPlayer player)
    {
        player.HandleDropPacket(FastDrop);
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(FastDrop);
    }
}
