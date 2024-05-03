using EFT;
using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
    public struct BTRInteractionPacket(int netId) : INetSerializable
    {
        public int NetId = netId;
        public bool HasInteractPacket = false;
        public PlayerInteractPacket InteractPacket;

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetInt();
            HasInteractPacket = reader.GetBool();
            if (HasInteractPacket)
            {
                InteractPacket = new()
                {
                    HasInteraction = reader.GetBool(),
                    InteractionType = (EInteractionType)reader.GetInt(),
                    SideId = reader.GetByte(),
                    SlotId = reader.GetByte(),
                    Fast = reader.GetBool()
                };
            }
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(NetId);
            writer.Put(HasInteractPacket);
            if (HasInteractPacket)
            {
                writer.Put(InteractPacket.HasInteraction);
                writer.Put((int)InteractPacket.InteractionType);
                writer.Put(InteractPacket.SideId);
                writer.Put(InteractPacket.SlotId);
                writer.Put(InteractPacket.Fast);
            }
        }
    }
}
