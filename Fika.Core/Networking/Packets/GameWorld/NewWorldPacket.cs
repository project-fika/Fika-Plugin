using LiteNetLib.Utils;
using System.Collections.Generic;

namespace Fika.Core.Networking
{
    public struct NewWorldPacket : INetSerializable
    {
        public List<RagdollPacketStruct> RagdollPackets;
        public List<ArtilleryPacketStruct> ArtilleryPackets;
        public List<GrenadeDataPacketStruct> GrenadePackets;
        public List<AirplaneDataPacketStruct> SyncObjectPackets;

        public readonly bool HasData
        {
            get
            {
                return RagdollPackets.Count > 0
                    || ArtilleryPackets.Count > 0
                    || GrenadePackets.Count > 0
                    || SyncObjectPackets.Count > 0;
            }
        }

        public void Deserialize(NetDataReader reader)
        {
            /*int ragdollPacketsCount = reader.GetInt();
            if (ragdollPacketsCount > 0)
            {
                RagdollPackets = [];
                for (int i = 0; i < ragdollPacketsCount; i++)
                {
                    RagdollPackets.Add(reader.GetRagdollStruct());
                }
            }*/

            int artilleryPacketsCount = reader.GetInt();
            if (artilleryPacketsCount > 0)
            {
                ArtilleryPackets = new(artilleryPacketsCount);
                for (int i = 0; i < artilleryPacketsCount; i++)
                {
                    ArtilleryPackets.Add(reader.GetArtilleryStruct());
                }
            }

            int grenadePacketsCount = reader.GetInt();
            if (grenadePacketsCount > 0)
            {
                GrenadePackets = new(grenadePacketsCount);
                for (int i = 0; i < grenadePacketsCount; i++)
                {
                    GrenadePackets.Add(reader.GetGrenadeStruct());
                }
            }

            int syncObjectPacketsCount = reader.GetInt();
            if (syncObjectPacketsCount > 0)
            {
                SyncObjectPackets = new(syncObjectPacketsCount);
                for (int i = 0; i < syncObjectPacketsCount; i++)
                {
                    SyncObjectPackets.Add(reader.GetAirplaneDataPacketStruct());
                }
            }
        }

        public void Serialize(NetDataWriter writer)
        {
            /*writer.Put(RagdollPackets.Count);
            for (int i = 0; i < RagdollPackets.Count; i++)
            {
                writer.PutRagdollStruct(RagdollPackets[i]);
            }*/

            writer.Put(ArtilleryPackets.Count);
            for (int i = 0; i < ArtilleryPackets.Count; i++)
            {
                writer.PutArtilleryStruct(ArtilleryPackets[i]);
            }

            writer.Put(GrenadePackets.Count);
            for (int i = 0; i < GrenadePackets.Count; i++)
            {
                writer.PutGrenadeStruct(GrenadePackets[i]);
            }

            writer.Put(SyncObjectPackets.Count);
            for (int i = 0; i < SyncObjectPackets.Count; i++)
            {
                writer.PutAirplaneDataPacketStruct(SyncObjectPackets[i]);
            }
        }

        public void Flush()
        {
            RagdollPackets.Clear();
            ArtilleryPackets.Clear();
            GrenadePackets.Clear();
        }
    }
}
