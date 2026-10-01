using Comfort.Common;
using EFT;
using Fika.Core.Main.Utils;

namespace Fika.Core.Networking.Packets.Generic.SubPackets;

public readonly struct SpawnAIPacket(int netId, Vector3 location) : IGenericPacket
{
    public EGenericPacketType Type => EGenericPacketType.SpawnAI;

    public readonly int NetId = netId;
    public readonly Vector3 Location = location;

    public readonly void Execute()
    {
        var coopHandler = Singleton<IFikaNetworkManager>.Instance.CoopHandler;
        if (coopHandler == null)
        {
            FikaGlobals.LogError("SpawnAI: CoopHandler was null!");
            return;
        }

        if (coopHandler.Players.TryGetValue(NetId, out var playerToApply))
        {
            if (playerToApply.Profile.Info.Settings.Role is WildSpawnType.shooterBTR)
            {
#if DEBUG
                FikaGlobals.LogWarning($"[{NetId}] was shooterBTR, skipping");
#endif
                return;
            }
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

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.PutUnmanaged(Location);
    }
}
