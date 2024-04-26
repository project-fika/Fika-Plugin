using LiteNetLib.Utils;
using UnityEngine;

namespace Fika.Core.Networking.Packets.GameWorld
{
    public struct MinePacket : INetSerializable
    {
        public Vector3 MinePositon;
        public void Deserialize(NetDataReader reader)
        {
            MinePositon = reader.GetVector3();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(MinePositon);
        }
    }
}
