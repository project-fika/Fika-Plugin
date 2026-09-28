using Fika.Core.Main.Players;

namespace Fika.Core.Networking.Packets.Player.Common.SubPackets;

public readonly struct RevivingPlayerPacket : IPlayerPacket
{
    public EPlayerPacketType Type => EPlayerPacketType.RevivingPlayer;

    public RevivingPlayerPacket(bool reviving, string nickname)
    {
        _reviving = reviving;
        _nickname = nickname;
    }

    public RevivingPlayerPacket(NetDataReader reader)
    {
        _reviving = reader.GetBool();
        _nickname = reader.GetString();
    }

    private readonly bool _reviving;
    private readonly string _nickname;

    public readonly void Execute(FikaPlayer player)
    {
        player.ToggleRevive(_reviving, _nickname);
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(_reviving);
        writer.Put(_nickname);
    }
}
