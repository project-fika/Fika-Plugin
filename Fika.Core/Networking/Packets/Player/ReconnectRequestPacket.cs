using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
    public struct ReconnectRequestPacket(string profileId, EReconnectPackgeType packageType): INetSerializable
    {
        public string ProfileId = profileId;
        public EReconnectPackgeType PackageType = packageType;

        public void Deserialize(NetDataReader reader)
        {
            ProfileId = reader.GetString();
            PackageType = (EReconnectPackgeType)reader.GetInt();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(ProfileId);
            writer.Put((int)PackageType);
        }
    }

    public enum EReconnectPackgeType
    {
        Everything,
        AirdropSetup,
        AirdropPositions
    }
}