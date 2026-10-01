using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;

namespace Fika.Core.Networking.Packets.Player.Common.SubPackets;

public readonly struct DownedSyncPacket(bool downed) : IPlayerPacket
{
    public EPlayerPacketType Type => EPlayerPacketType.DownedSync;

    public readonly bool Downed = downed;

    public readonly void Execute(FikaPlayer player)
    {
        if (player is ObservedPlayer observedPlayer)
        {
            observedPlayer.ToggleDowned(Downed);
            return;
        }

        FikaGlobals.LogError($"OnHealthSyncPacketReceived::Player with id {player.NetId} was not observed. Name: {player.Profile.GetCorrectedNickname()}");
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(Downed);
    }
}
