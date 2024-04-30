using LiteNetLib.Utils;
using static Fika.Core.Networking.FikaSerialization;

namespace Fika.Core.Networking
{
    public struct DamagePacket(int netId) : INetSerializable
    {
        public int NetId = netId;
        public ApplyShotPacket DamageInfo;

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetInt();
            DamageInfo = ApplyShotPacket.Deserialize(reader);
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(NetId);
            ApplyShotPacket.Serialize(writer, DamageInfo);
        }
    }
}
