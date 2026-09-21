using Fika.Core.Main.Players;

namespace Fika.Core.Networking.Packets.Player.Common.SubPackets;

public readonly struct FastForwardOutdatedPacket : IPlayerPacket
{
    public EPlayerPacketType Type => EPlayerPacketType.FastForwardOutdated;

    public readonly void Execute(FikaPlayer player)
    {
        if (player.HandsController != null)
        {
            player.HandsController.FastForwardCurrentState();
        }
    }

    public readonly void Serialize(NetDataWriter writer)
    {
    }
}
