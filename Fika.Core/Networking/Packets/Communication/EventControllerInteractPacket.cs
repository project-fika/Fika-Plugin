using EFT;
using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
    public class EventControllerInteractPacket : INetSerializable
    {
        public ushort NetId;
        public InteractPacketStruct Data;

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetUShort();
            Data = new()
            {
                hasInteraction = reader.GetBool(),
                objectId = reader.GetInt(),
                interaction = (EventObject.EInteraction)reader.GetByte()
            };
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(NetId);
            writer.Put(Data.hasInteraction);
            writer.Put(Data.objectId);
            writer.Put((byte)Data.interaction);
        }
    }
}
