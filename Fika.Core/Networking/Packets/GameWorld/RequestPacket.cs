using LiteNetLib.Utils;
using static Fika.Core.Networking.RequestSubPackets;
using static Fika.Core.Networking.SubPacket;

namespace Fika.Core.Networking
{
    public class RequestPacket : INetSerializable
    {
        public ERequestSubPacketType PacketType;
        /// <summary>
        /// Do not assign manually, this is handled by the packet during <see cref="Serialize(NetDataWriter)"/>
        /// </summary>
        private bool hasSubPacket;
        public IRequestPacket RequestSubPacket;

        public void Deserialize(NetDataReader reader)
        {
            PacketType = (ERequestSubPacketType)reader.GetByte();
            hasSubPacket = reader.GetBool();
            if (hasSubPacket)
            {
                RequestSubPacket = FikaSerializationExtensions.GetRequestSubPacket(reader, PacketType);
                return;
            }

            RequestSubPacket = GenerateDefaultSubPacket(PacketType);
        }

        private IRequestPacket GenerateDefaultSubPacket(ERequestSubPacketType packetType)
        {
            switch (packetType)
            {
                case ERequestSubPacketType.SpawnPoint:
                    return new SpawnPointRequest();
                case ERequestSubPacketType.Weather:
                    return new WeatherRequest();
                case ERequestSubPacketType.Exfiltration:
                    return new ExfiltrationRequest();
                case ERequestSubPacketType.TraderServices:
                    FikaPlugin.Instance.FikaLogger.LogError("RequestPacket::GenerateDefaultSubPacket: Type was TraderServices which should never be null!");
                    break;
                default:
                    FikaPlugin.Instance.FikaLogger.LogError("RequestPacket::GenerateDefaultSubPacket: Type was out of bounds!");
                    break;
            }

            return default;
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put((byte)PacketType);
            hasSubPacket = RequestSubPacket != null;
            writer.Put(hasSubPacket);
            if (hasSubPacket)
            {
                RequestSubPacket.Serialize(writer);
            }
        }
    }
}
