using Comfort.Common;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.Generic.SubPackets;

public sealed class SpawnAI : IPoolSubPacket
{
    public int NetId;
    public Vector3 Location;

    private SpawnAI() { }

    public static SpawnAI CreateInstance()
    {
        return new SpawnAI();
    }

    public static SpawnAI FromValue(int netId, Vector3 location)
    {
        var packet = GenericSubPacketPoolManager.Instance.GetPacket<SpawnAI>(EGenericSubPacketType.SpawnAI);
        packet.NetId = netId;
        packet.Location = location;
        return packet;
    }

    public void Execute(FikaPlayer player = null)
    {
        var coopHandler = Singleton<IFikaNetworkManager>.Instance.CoopHandler;
        if (coopHandler == null)
        {
            FikaGlobals.LogError("SpawnAI: CoopHandler was null!");
            return;
        }

        if (coopHandler.Players.TryGetValue(NetId, out var playerToApply))
        {
#if DEBUG
            FikaGlobals.LogWarning($"[{NetId}] is ready, spawning at {Location}");
#endif
            playerToApply.Teleport(Location);
        }
        else
        {
            FikaGlobals.LogWarning($"Could not find {NetId} to teleport");
        }
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.PutUnmanaged(Location);
    }

    public void Deserialize(NetDataReader reader)
    {
        NetId = reader.GetInt();
        Location = reader.GetUnmanaged<Vector3>();
    }

    public void Dispose()
    {
        NetId = 0;
        Location = default;
    }
}
