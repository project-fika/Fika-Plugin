using Fika.Core.Networking.Packets;
using LiteNetLib.Utils;
using System.Collections.Generic;

namespace Fika.Core.Networking
{
    public class WorldPacket : IReusable
    {
        public List<RagdollPacketStruct> RagdollPackets = [];
        public List<ArtilleryPacketStruct> ArtilleryPackets = [];
        public List<GrenadeDataPacketStruct> GrenadePackets = [];
        public List<AirplaneDataPacketStruct> SyncObjectPackets = [];
        public List<LootSyncStruct> LootSyncStructs = [];

        public bool HasData
        {
            get
            {
                return RagdollPackets.Count > 0
                    || ArtilleryPackets.Count > 0
                    || GrenadePackets.Count > 0
                    || SyncObjectPackets.Count > 0
                    || LootSyncStructs.Count > 0;
            }
        }

        public void Flush()
        {
            RagdollPackets.Clear();
            ArtilleryPackets.Clear();
            GrenadePackets.Clear();
            SyncObjectPackets.Clear();
            LootSyncStructs.Clear();
        }
    }
}
