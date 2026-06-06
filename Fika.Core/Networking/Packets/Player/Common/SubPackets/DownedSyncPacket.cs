using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.Player.Common.SubPackets;

public sealed class DownedSyncPacket : IPoolSubPacket
{
    private DownedSyncPacket() { }

    public static DownedSyncPacket CreateInstance()
    {
        return new();
    }

    public bool Downed;

    public static DownedSyncPacket FromValue(bool downed)
    {
        var packet = CommonSubPacketPoolManager.Instance.GetPacket<DownedSyncPacket>(ECommonSubPacketType.DownedSync);
        packet.Downed = downed;
        return packet;
    }

    public void Execute(FikaPlayer player = null)
    {
        if (player is ObservedPlayer observedPlayer)
        {
            observedPlayer.ToggleDowned(Downed);
            return;
        }

        FikaGlobals.LogError($"OnHealthSyncPacketReceived::Player with id {player.NetId} was not observed. Name: {player.Profile.GetCorrectedNickname()}");
    }

    public void Deserialize(NetDataReader reader)
    {
        Downed = reader.GetBool();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Downed);
    }

    public void Dispose()
    {
        Downed = default;
    }
}
