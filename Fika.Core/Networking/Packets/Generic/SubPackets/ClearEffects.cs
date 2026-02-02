using Comfort.Common;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.Generic.SubPackets;

public sealed class ClearEffects : IPoolSubPacket
{
    public int NetId;

    private ClearEffects() { }

    public static ClearEffects CreateInstance()
    {
        return new ClearEffects();
    }

    public static ClearEffects FromValue(int netId)
    {
        var packet = GenericSubPacketPoolManager.Instance.GetPacket<ClearEffects>(EGenericSubPacketType.ClearEffects);
        packet.NetId = netId;
        return packet;
    }

    public void Execute(FikaPlayer player = null)
    {
        if (FikaBackendUtils.IsServer)
        {
            return;
        }

        var coopHandler = Singleton<IFikaNetworkManager>.Instance.CoopHandler;
        if (coopHandler == null)
        {
            FikaGlobals.LogError("ClientExtract: CoopHandler was null!");
            return;
        }

        if (coopHandler.Players.TryGetValue(NetId, out var playerToApply))
        {
            if (playerToApply is ObservedPlayer observedPlayer)
            {
                observedPlayer.HealthBar.ClearEffects();
            }
        }
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
    }

    public void Deserialize(NetDataReader reader)
    {
        NetId = reader.GetInt();
    }

    public void Dispose()
    {
        NetId = 0;
    }
}
