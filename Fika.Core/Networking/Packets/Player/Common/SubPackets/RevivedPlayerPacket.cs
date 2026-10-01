using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;

namespace Fika.Core.Networking.Packets.Player.Common.SubPackets;

public readonly struct RevivedPlayerPacket : IPlayerPacket
{
    public EPlayerPacketType Type => EPlayerPacketType.RevivedPlayer;

    public readonly void Execute(FikaPlayer player)
    {
        if (player.IsYourPlayer)
        {
            player.ToggleDowned(false);
            return;
        }

        FikaGlobals.LogError($"OnHealthSyncPacketReceived::Player with id {player.NetId} was not local. Name: {player.Profile.GetCorrectedNickname()}");
    }

    public readonly void Serialize(NetDataWriter writer)
    {

    }
}
