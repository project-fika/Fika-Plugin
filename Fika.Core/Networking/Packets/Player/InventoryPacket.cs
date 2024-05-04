using LiteNetLib.Utils;
using static Fika.Core.Networking.FikaSerialization;

namespace Fika.Core.Networking
{
    public struct InventoryPacket(int netId) : INetSerializable
    {
        public int NetId = netId;
        public bool HasItemControllerExecutePacket = false;
        public ItemControllerExecutePacket ItemControllerExecutePacket;
        public bool HasSearchPacket = false;
        public SearchPacket SearchPacket;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(NetId);
            writer.Put(HasItemControllerExecutePacket);
            if (HasItemControllerExecutePacket)
                ItemControllerExecutePacket.Serialize(writer, ItemControllerExecutePacket);
            writer.Put(HasSearchPacket);
            if (HasSearchPacket)
                SearchPacket.Serialize(writer, SearchPacket);
        }

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetInt();
            HasItemControllerExecutePacket = reader.GetBool();
            if (HasItemControllerExecutePacket)
                ItemControllerExecutePacket = ItemControllerExecutePacket.Deserialize(reader);
            HasSearchPacket = reader.GetBool();
            if (HasSearchPacket)
                SearchPacket = SearchPacket.Deserialize(reader);
        }
    }
}
