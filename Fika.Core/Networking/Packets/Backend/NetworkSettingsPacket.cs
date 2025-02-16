using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
    public struct NetworkSettingsPacket : INetSerializable
    {
        public int SendRate;
        public int NetId;

        public void Deserialize(NetDataReader reader)
        {
            SendRate = reader.GetInt();
            NetId = reader.GetInt();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(SendRate);
            writer.Put(NetId);
        }
    }
}
