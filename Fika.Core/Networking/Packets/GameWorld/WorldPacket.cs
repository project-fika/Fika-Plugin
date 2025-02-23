/*using System.Collections.Generic;

namespace Fika.Core.Networking
{
    public class WorldPacket : IReusable
    {
        public List<RagdollPacketStruct> RagdollPackets { get; set; } = [];
        public List<ArtilleryPacketStruct> ArtilleryPackets { get; set; } = [];
        public List<GrenadeDataPacketStruct> ThrowablePackets { get; set; } = [];

        public bool HasData
        {
            get
            {
                return RagdollPackets.Count > 0
                    || ArtilleryPackets.Count > 0
                    || ThrowablePackets.Count > 0;
            }
        }

        public void Flush()
        {
            RagdollPackets.Clear();
            ArtilleryPackets.Clear();
            ThrowablePackets.Clear();
        }
    }
}
*/