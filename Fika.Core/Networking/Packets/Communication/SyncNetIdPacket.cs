using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
    public struct SyncNetIdPacket(string profileId, int netId) : INetSerializable
    {
        public string ProfileId = profileId;
        public int NetId = netId;

        public void Deserialize(NetDataReader reader)
        {
            ProfileId = reader.GetString();
            NetId = reader.GetInt();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(ProfileId);
            writer.Put(NetId);
        }
    }
}
