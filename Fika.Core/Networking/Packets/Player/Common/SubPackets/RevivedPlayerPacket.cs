using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;

namespace Fika.Core.Networking.Packets.Player.Common.SubPackets;

public readonly struct RevivedPlayerPacket
{
    public readonly void Execute(FikaPlayer player = null)
    {
        if (player != null)
        {
            if (player.IsYourPlayer)
            {
                player.ToggleDowned(false);
            }

            return;
        }

        FikaGlobals.LogError($"OnHealthSyncPacketReceived::Player with id {player.NetId} was not local. Name: {player.Profile.GetCorrectedNickname()}");
    }
}
