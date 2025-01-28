using LiteNetLib.Utils;
using static Fika.Core.Networking.Packets.SubPacket;

namespace Fika.Core.Networking.Packets.GameWorld
{
    public class RequestPacket : INetSerializable
    {
        public ERequestSubPacketType PacketType;
        public bool Request;
        public IRequestPacket RequestSubPacket;

        public void Deserialize(NetDataReader reader)
        {
            PacketType = (ERequestSubPacketType)reader.GetByte();
            Request = reader.GetBool();
            if (!Request)
            {
                RequestSubPacket = FikaSerializationExtensions.GetRequestSubPacket(reader, PacketType);
            }
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put((byte)PacketType);
            writer.Put(Request);
            if (!Request)
            {
                RequestSubPacket.Serialize(writer);
            }
        }
    }
}
