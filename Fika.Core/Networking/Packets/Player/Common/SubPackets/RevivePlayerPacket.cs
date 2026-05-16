using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.Player.Common.SubPackets;

public sealed class RevivePlayerPacket : IPoolSubPacket
{
    private RevivePlayerPacket() { }

    public static RevivePlayerPacket CreateInstance()
    {
        return new();
    }

    public static RevivePlayerPacket FromValue()
    {
        return CommonSubPacketPoolManager.Instance.GetPacket<RevivePlayerPacket>(ECommonSubPacketType.RevivePlayer);
    }

    public void Execute(FikaPlayer player = null)
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

    public void Deserialize(NetDataReader reader)
    {
        
    }

    public void Serialize(NetDataWriter writer)
    {
        
    }

    public void Dispose()
    {

    }
}
