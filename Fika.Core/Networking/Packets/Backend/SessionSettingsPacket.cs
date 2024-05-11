using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
    public struct SessionSettingsPacket(bool isRequest) : INetSerializable
    {
        public bool IsRequest = isRequest;
        public bool MetabolismDisabled;

        public void Deserialize(NetDataReader reader)
        {
            IsRequest = reader.GetBool();
            if (!IsRequest)
            {
                MetabolismDisabled = reader.GetBool(); 
            }
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(IsRequest);
            if (!IsRequest)
            {
                writer.Put(MetabolismDisabled); 
            }
        }
    }
}
