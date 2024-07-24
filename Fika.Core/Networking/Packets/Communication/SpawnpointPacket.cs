using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
    public struct SpawnpointPacket(bool isRequest) : INetSerializable
    {
        public bool IsRequest = isRequest;
        public string Name;

        public void Deserialize(NetDataReader reader)
        {
            IsRequest = reader.GetBool();
            Name = reader.GetString();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(IsRequest);
            writer.Put(Name);
        }
    }
}
