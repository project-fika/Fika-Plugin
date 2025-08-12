using LiteNetLib.Utils;

namespace Fika.Core.Networking.Packets.Backend;

public struct StatisticsPacket(int serverFps) : INetSerializable
{
    public int ServerFPS = serverFps;

    public void Deserialize(NetDataReader reader)
    {
        ServerFPS = reader.GetInt();
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(ServerFPS);
    }
}
