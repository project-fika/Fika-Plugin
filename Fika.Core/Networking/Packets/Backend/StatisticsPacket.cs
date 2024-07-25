using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
    public struct StatisticsPacket(int serverFps) : INetSerializable
    {
        public int ServerFPS = serverFps;

        public void Deserialize(NetDataReader reader)
        {
            ServerFPS = reader.GetInt();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(ServerFPS);
        }
    }
}
