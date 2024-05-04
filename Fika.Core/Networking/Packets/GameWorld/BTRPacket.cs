using LiteNetLib.Utils;
using UnityEngine;
using static Fika.Core.Networking.FikaSerialization;

namespace Fika.Core.Networking
{
    public struct BTRPacket() : INetSerializable
    {
        public BTRDataPacket BTRDataPacket;
        public bool HasBotProfileId = false;
        public int BotNetId;
        public bool HasShot = false;
        public Vector3 ShotPosition;
        public Vector3 ShotDirection;

        public void Deserialize(NetDataReader reader)
        {
            BTRDataPacket = BTRDataPacketUtils.Deserialize(reader);
            HasBotProfileId = reader.GetBool();
            if (HasBotProfileId)
                BotNetId = reader.GetInt();
            HasShot = reader.GetBool();
            if (HasShot)
            {
                ShotPosition = reader.GetVector3();
                ShotDirection = reader.GetVector3();
            }
        }

        public void Serialize(NetDataWriter writer)
        {
            BTRDataPacketUtils.Serialize(writer, BTRDataPacket);
            writer.Put(HasBotProfileId);
            if (HasBotProfileId)
                writer.Put(BotNetId);
            writer.Put(HasShot);
            if (HasShot)
            {
                writer.Put(ShotPosition);
                writer.Put(ShotDirection);
            }
        }
    }
}
