using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
    public struct ReconnectRequestPacket(int netId): INetSerializable
    {
        public int NetId = netId;

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetInt();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(NetId);
        }
    }
}