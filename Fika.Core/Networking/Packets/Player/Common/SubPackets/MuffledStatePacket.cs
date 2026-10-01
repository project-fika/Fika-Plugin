using Fika.Core.Main.Players;

namespace Fika.Core.Networking.Packets.Player.Common.SubPackets;

public readonly struct MuffledStatePacket(bool muffled) : IPlayerPacket
{
    public EPlayerPacketType Type => EPlayerPacketType.MuffledState;

    public readonly bool Muffled = muffled;

    public readonly void Execute(FikaPlayer player)
    {
        if (player is ObservedPlayer observed)
        {
            observed.SetMuffledState(Muffled);
        }
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(Muffled);
    }
}
