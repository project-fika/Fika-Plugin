/*using System.Collections.Generic;

namespace Fika.Core.Networking
{
    public class SyncObjectPacket : IReusable
    {
        public List<AirplaneDataPacketStruct> Packets { get; set; } = [];

        public bool HasData
        {
            get
            {
                return Packets.Count > 0;
            }
        }

        public void Flush()
        {
            Packets.Clear();
        }
    }
}
*/