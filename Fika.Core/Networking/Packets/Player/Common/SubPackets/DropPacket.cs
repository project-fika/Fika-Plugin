using Fika.Core.Main.Players;

namespace Fika.Core.Networking.Packets.Player.Common.SubPackets;

public readonly struct DropPacket(bool fastDrop) : IPlayerPacket
{
    public EPlayerPacketType Type => EPlayerPacketType.Drop;

    public readonly bool FastDrop = fastDrop;

    public readonly void Execute(FikaPlayer player)
    {
        player.HandleDropPacket(FastDrop);
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(FastDrop);
    }
}
