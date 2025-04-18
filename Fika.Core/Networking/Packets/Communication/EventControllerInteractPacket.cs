using EFT;
using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
    public class EventControllerInteractPacket : INetSerializable
    {
        public int NetId;
        public GStruct183 Data;

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetInt();
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
