using JsonType;
using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
    public struct TransitInteractPacket : INetSerializable
    {
        public ushort NetId;
        public TransitInteractionPacketStruct Data;

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetUShort();
            Data = new()
            {
                hasInteraction = true,
                pointId = reader.GetInt(),
                keyId = reader.GetString(),
                time = (EDateTime)reader.GetByte()
            };
        }

        public readonly void Serialize(NetDataWriter writer)
        {
            writer.Put(NetId);
            writer.Put(Data.pointId);
            writer.Put(Data.keyId);
            writer.Put((byte)Data.time);
        }
    }
}
