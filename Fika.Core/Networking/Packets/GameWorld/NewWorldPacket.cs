using Fika.Core.Coop.Utils;
using LiteNetLib.Utils;
using System.Collections.Generic;

namespace Fika.Core.Networking
{
    public struct NewWorldPacket : INetSerializable
    {
        public List<RagdollPacketStruct> RagdollPackets;
        public List<ArtilleryPacketStruct> ArtilleryPackets;
        public List<GrenadeDataPacketStruct> GrenadePackets;

        public readonly bool HasData
        {
            get
            {
                return RagdollPackets.Count > 0
                    || ArtilleryPackets.Count > 0
                    || GrenadePackets.Count > 0;
            }
        }

        public void Deserialize(NetDataReader reader)
        {
            /*int ragdollPacketsCount = reader.GetInt();
            if (ragdollPacketsCount > 0)
            {
                FikaGlobals.LogInfo($"I received {ragdollPacketsCount} ragdoll packets");
                RagdollPackets = [];
                for (int i = 0; i < ragdollPacketsCount; i++)
                {
                    RagdollPackets.Add(reader.GetRagdollStruct());
                }
            }*/

            int artilleryPacketsCount = reader.GetInt();
            if (artilleryPacketsCount > 0)
            {
                ArtilleryPackets = [];
                for (int i = 0; i < artilleryPacketsCount; i++)
                {
                    ArtilleryPackets.Add(reader.GetArtilleryStruct());
                }
            }

            int grenadePacketsCount = reader.GetInt();
            if (grenadePacketsCount > 0)
            {
                FikaGlobals.LogInfo($"I received {grenadePacketsCount} grenade packets");
                GrenadePackets = [];
                for (int i = 0; i < grenadePacketsCount; i++)
                {
                    GrenadePackets.Add(reader.GetGrenadeStruct());
                }
            }
        }

        public void Serialize(NetDataWriter writer)
        {
            /*writer.Put(RagdollPackets.Count);
            for (int i = 0; i < RagdollPackets.Count; i++)
            {
                FikaGlobals.LogInfo("Writing ragdoll struct");
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
                FikaGlobals.LogInfo("Writing grenade struct");
                writer.PutGrenadeStruct(GrenadePackets[i]);
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
