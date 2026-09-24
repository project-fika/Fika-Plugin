using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;

namespace Fika.Core.Networking.Packets.Player.Common.SubPackets;

public readonly struct DownedSyncPacket
{
    public DownedSyncPacket(bool downed)
    {
        Downed = downed;
    }

    public readonly bool Downed;

    public readonly void Execute(FikaPlayer player = null)
    {
        if (player is ObservedPlayer observedPlayer)
        {
            observedPlayer.ToggleDowned(Downed);
            return;
        }

        FikaGlobals.LogError($"OnHealthSyncPacketReceived::Player with id {player.NetId} was not observed. Name: {player.Profile.GetCorrectedNickname()}");
    }
}
