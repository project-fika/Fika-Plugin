using LiteNetLib.Utils;
using static Fika.Core.Networking.FikaSerialization;

namespace Fika.Core.Networking
{
    public struct DamagePacket(string profileId) : INetSerializable
    {
        public string ProfileId = profileId;
        public ApplyShotPacket DamageInfo;

        public void Deserialize(NetDataReader reader)
        {
            ProfileId = reader.GetString();
            DamageInfo = ApplyShotPacket.Deserialize(reader);
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(ProfileId);
            ApplyShotPacket.Serialize(writer, DamageInfo);
        }
    }
}
