using Fika.Core.Main.Factories;
using LiteNetLib.Utils;
using UnityEngine;

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
            PingLocation = reader.GetVector3();
            PingType = (PingFactory.EPingType)reader.GetByte();
            PingColor = reader.GetColor();
            Nickname = reader.GetString();
            LocaleId = reader.GetString();
        }

        public readonly void Serialize(NetDataWriter writer)
        {
            writer.PutVector3(PingLocation);
            writer.Put((byte)PingType);
            writer.PutColor(PingColor);
            writer.Put(Nickname);
            writer.Put(LocaleId);
        }
    }
}
