using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
    internal struct BorderZonePacket : INetSerializable
    {
        public string ProfileId;
        public int ZoneId;

        public void Deserialize(NetDataReader reader)
        {
            ProfileId = reader.GetString();
            ZoneId = reader.GetInt();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(ProfileId);
            writer.Put(ZoneId);
        }
    }
}
