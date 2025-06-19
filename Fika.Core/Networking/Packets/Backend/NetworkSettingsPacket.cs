using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
    public struct NetworkSettingsPacket : INetSerializable
    {
        public string ProfileId;

        public int SendRate;
        public int NetId;
        public bool AllowVOIP;

        public void Deserialize(NetDataReader reader)
        {
            if (reader.GetBool())
            {
                ProfileId = reader.GetString();
                return;
            }

            SendRate = reader.GetInt();
            NetId = reader.GetInt();
            AllowVOIP = reader.GetBool();
        }

        public readonly void Serialize(NetDataWriter writer)
        {
            bool isRequest = !string.IsNullOrEmpty(ProfileId);
            writer.Put(isRequest);

            if (isRequest)
            {
                writer.Put(ProfileId);
                return;
            }

            writer.Put(SendRate);
            writer.Put(NetId);
            writer.Put(AllowVOIP);
        }
    }
}
