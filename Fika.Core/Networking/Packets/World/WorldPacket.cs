using EFT;
using System.Collections.Generic;

namespace Fika.Core.Networking.Packets.World;

public sealed class WorldPacket : IReusable
{
    public List<ArtilleryProjectileSyncPacket> ArtilleryPackets { get; set; }
    public List<GrenadeSyncPacket> GrenadePackets { get; set; }
    public List<SynchronizableObjectPacket> SyncObjectPackets { get; set; }
    public List<EFT.LootSyncPacket> LootSyncStructs { get; set; }

    public bool HasData
    {
        get
        {
            return ArtilleryPackets.Count > 0
                || GrenadePackets.Count > 0
                || SyncObjectPackets.Count > 0
                || LootSyncStructs.Count > 0;
        }
    }

    public void Flush()
    {
        ArtilleryPackets.Clear();
        GrenadePackets.Clear();
        SyncObjectPackets.Clear();
        LootSyncStructs.Clear();
    }
}
