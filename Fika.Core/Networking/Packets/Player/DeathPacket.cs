using LiteNetLib.Utils;
using static Fika.Core.Networking.FikaSerialization;

namespace Fika.Core.Networking
{
    public struct DeathPacket(int netId) : INetSerializable
    {
        public int NetId = netId;
        public RagdollPacket RagdollPacket;
        public bool HasInventory = false;
        public EquipmentClass Equipment;

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetInt();
            RagdollPacket = RagdollPacket.Deserialize(reader);
            HasInventory = reader.GetBool();
            if (HasInventory)
            {
                Equipment = (EquipmentClass)reader.GetItem();
            }
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(NetId);
            RagdollPacket.Serialize(writer, RagdollPacket);
            writer.Put(HasInventory);
            if (HasInventory)
            {
                writer.PutItem(Equipment);
            }
        }
    }
}
