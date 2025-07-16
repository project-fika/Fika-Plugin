using Fika.Core.Main.Utils;
using LiteNetLib.Utils;
using static Fika.Core.Networking.Packets.World.RequestSubPackets;
using static Fika.Core.Networking.Packets.SubPacket;

namespace Fika.Core.Networking
{
    public class RequestPacket : INetSerializable
    {
        public ERequestSubPacketType Type;
        /// <summary>
        /// Do not assign manually, this is handled by the packet during <see cref="Serialize(NetDataWriter)"/>
        /// </summary>
        private bool _hasSubPacket;
        public IRequestPacket RequestSubPacket;

        public void Deserialize(NetDataReader reader)
        {
            Type = reader.GetEnum<ERequestSubPacketType>();
            _hasSubPacket = reader.GetBool();
            if (_hasSubPacket)
            {
                RequestSubPacket = FikaSerializationExtensions.GetRequestSubPacket(reader, Type);
                return;
            }

            RequestSubPacket = GenerateDefaultSubPacket(Type);
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
                    FikaGlobals.LogError("Type was TraderServices which should never be null!");
                    break;
                case ERequestSubPacketType.CharacterSync:
                    FikaGlobals.LogError("Type was CharacterSync which should never be null!");
                    break;
                default:
                    FikaGlobals.LogError("Type was out of bounds!");
                    break;
            }

            return default;
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.PutEnum(Type);
            _hasSubPacket = RequestSubPacket != null;
            writer.Put(_hasSubPacket);
            if (_hasSubPacket)
            {
                RequestSubPacket.Serialize(writer);
            }
        }
    }
}
