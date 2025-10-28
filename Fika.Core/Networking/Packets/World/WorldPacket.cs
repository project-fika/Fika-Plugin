﻿using System.Collections.Generic;

namespace Fika.Core.Networking.Packets.World;

public class WorldPacket : IReusable
{
    public List<ArtilleryPacketStruct> ArtilleryPackets { get; set; }
    public List<GrenadeDataPacketStruct> GrenadePackets { get; set; }
    public List<AirplaneDataPacketStruct> SyncObjectPackets { get; set; }
    public List<LootSyncStruct> LootSyncStructs { get; set; }

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
