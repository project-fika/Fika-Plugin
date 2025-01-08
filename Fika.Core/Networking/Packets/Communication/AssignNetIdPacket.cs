using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
    public struct AssignNetIdPacket : INetSerializable
    {
        public int NetId;

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetInt();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(NetId);
        }
    }
}
