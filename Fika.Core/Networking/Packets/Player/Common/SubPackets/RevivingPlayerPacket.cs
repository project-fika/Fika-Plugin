using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;

namespace Fika.Core.Networking.Packets.Player.Common.SubPackets;

public readonly struct RevivingPlayerPacket
{
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

    public readonly void Execute(FikaPlayer player = null)
    {
        if (player != null)
        {
            player.ToggleRevive(_reviving, _nickname);
            return;
        }

        FikaGlobals.LogError($"OnHealthSyncPacketReceived::Player with id {player.NetId} was not local. Name: {player.Profile.GetCorrectedNickname()}");
    }



    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(_reviving);
        writer.Put(_nickname);
    }
}
