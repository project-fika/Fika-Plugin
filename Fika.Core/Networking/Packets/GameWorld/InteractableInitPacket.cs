using ComponentAce.Compression.Libs.zlib;
using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
    public struct InteractableInitPacket(bool isRequest) : INetSerializable
    {
        public bool IsRequest = isRequest;
        public byte[] RawData;
        public int Length;
        public string[] InteractableIds;
        public int[] NetIds;

        public void Deserialize(NetDataReader reader)
        {
            IsRequest = reader.GetBool();
            if (!IsRequest)
            {
                RawData = reader.GetByteArray();
                InteractableInitPacket interactableInitPacket = SimpleZlib.Decompress(RawData, null).ParseJsonTo<InteractableInitPacket>([]);
                Length = interactableInitPacket.Length;
                InteractableIds = interactableInitPacket.InteractableIds;
                NetIds = interactableInitPacket.NetIds;
            }
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(IsRequest);
            if (!IsRequest)
            {
                byte[] data = SimpleZlib.CompressToBytes(this.ToJson([]), 6);
                writer.PutByteArray(data);
            }
        }
    }
}
