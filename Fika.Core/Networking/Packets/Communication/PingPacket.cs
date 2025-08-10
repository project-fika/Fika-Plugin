using Fika.Core.Main.Factories;
using LiteNetLib.Utils;

namespace Fika.Core.Networking.Packets.Communication
{
    public struct PingPacket : INetSerializable
    {
        public Vector3 PingLocation;
        public PingFactory.EPingType PingType;
        public Color PingColor;
        public string Nickname;
        public string LocaleId;

        public void Deserialize(NetDataReader reader)
        {
            PingLocation = reader.GetStruct<Vector3>();
            PingType = (PingFactory.EPingType)reader.GetByte();
            PingColor = reader.GetStruct<Color>();
            Nickname = reader.GetString();
            LocaleId = reader.GetString();
        }

        public readonly void Serialize(NetDataWriter writer)
        {
            writer.PutStruct(PingLocation);
            writer.Put((byte)PingType);
            writer.PutStruct(PingColor);
            writer.Put(Nickname);
            writer.Put(LocaleId);
        }
    }
}
