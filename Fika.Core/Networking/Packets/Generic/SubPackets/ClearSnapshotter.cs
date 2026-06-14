using Comfort.Common;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.Generic.SubPackets;

public sealed class ClearSnapshotter : IPoolSubPacket
{
    public int NetId;

    private ClearSnapshotter() { }

    public static ClearSnapshotter CreateInstance()
    {
        return new ClearSnapshotter();
    }

    public static ClearSnapshotter FromValue(int netId)
    {
        var packet = GenericSubPacketPoolManager.Instance.GetPacket<ClearSnapshotter>(EGenericSubPacketType.ClearSnapshotter);
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
            FikaGlobals.LogError("ClearSnapshotter: CoopHandler was null!");
            return;
        }

        if (coopHandler.Players.TryGetValue(NetId, out var playerToApply) && playerToApply is ObservedPlayer observedPlayer)
        {
            observedPlayer.Snapshotter.Clear();
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
