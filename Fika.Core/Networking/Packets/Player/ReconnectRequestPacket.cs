using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
    public struct ReconnectRequestPacket(string profileId): INetSerializable
    {
        public string ProfileId = profileId;

        public void Deserialize(NetDataReader reader)
        {
            ProfileId = reader.GetString();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(ProfileId);
        }
    }
}