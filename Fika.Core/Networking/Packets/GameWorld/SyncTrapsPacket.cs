using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
    public struct SyncTrapsPacket : INetSerializable
    {
        public byte[] Data;

        public void Deserialize(NetDataReader reader)
        {
            Data = reader.GetByteArray();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.PutByteArray(Data);
        }
    }
}
