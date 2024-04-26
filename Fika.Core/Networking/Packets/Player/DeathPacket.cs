using LiteNetLib.Utils;
using static Fika.Core.Networking.FikaSerialization;

namespace Fika.Core.Networking
{
    public struct DeathPacket(string profileId) : INetSerializable
    {
        public string ProfileId = profileId;
        public RagdollPacket RagdollPacket;
        public bool HasInventory = false;
        public EquipmentClass Equipment;

        public void Deserialize(NetDataReader reader)
        {
            ProfileId = reader.GetString();
            RagdollPacket = RagdollPacket.Deserialize(reader);
            HasInventory = reader.GetBool();
            if (HasInventory)
            {
                Equipment = (EquipmentClass)reader.GetItem();
            }
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(ProfileId);
            RagdollPacket.Serialize(writer, RagdollPacket);
            writer.Put(HasInventory);
            if (HasInventory)
            {
                writer.PutItem(Equipment);
            }
        }
    }
}
