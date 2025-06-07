using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fika.Core.Networking.Packets.Debug
{
    public class CommandPacket : INetSerializable
    {


        public void Deserialize(NetDataReader reader)
        {
            throw new NotImplementedException();
        }

        public void Serialize(NetDataWriter writer)
        {
            throw new NotImplementedException();
        }

        public enum ECommandType
        {
            SpawnAI,
            ClearAI,
            Bring
        }
    }
}
