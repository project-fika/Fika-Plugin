// © 2024 Lacyway All Rights Reserved

using EFT;
using Fika.Core.Coop.Factories;
using LiteNetLib.Utils;
using UnityEngine;

namespace Fika.Core.Networking
{
    /// <summary>
    /// Packet used for many different things to reduce packet bloat
    /// </summary>
    /// <param name="packageType"></param>
    public struct GenericPacket(EPackageType packageType) : INetSerializable
    {
        public string ProfileId;
        public EPackageType PacketType = packageType;
        public Vector3 PingLocation;
        public PingFactory.EPingType PingType;
        public Color PingColor = Color.white;
        public string Nickname;
        public string BotProfileId;
        public long DepartureTime;
        public string ExfilName;
        public float ExfilStartTime;
        public ETraderServiceType TraderServiceType;

        public void Deserialize(NetDataReader reader)
        {
            ProfileId = reader.GetString();
            PacketType = (EPackageType)reader.GetInt();
            switch (PacketType)
            {
                case EPackageType.Ping:
                    PingLocation = reader.GetVector3();
                    PingType = (PingFactory.EPingType)reader.GetByte();
                    PingColor = reader.GetColor();
                    Nickname = reader.GetString();
                    break;
                case EPackageType.LoadBot:
                    BotProfileId = reader.GetString();
                    break;
                case EPackageType.TrainSync:
                    DepartureTime = reader.GetLong();
                    break;
                case EPackageType.ExfilCountdown:
                    ExfilName = reader.GetString();
                    ExfilStartTime = reader.GetFloat();
                    break;
                case EPackageType.TraderServiceNotification:
                    TraderServiceType = (ETraderServiceType)reader.GetInt();
                    break;
                case EPackageType.DisposeBot:
                    BotProfileId = reader.GetString();
                    break;
            }
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(ProfileId);
            writer.Put((int)PacketType);
            switch (PacketType)
            {
                case EPackageType.Ping:
                    writer.Put(PingLocation);
                    writer.Put((byte)PingType);
                    writer.Put(PingColor);
                    writer.Put(Nickname);
                    break;
                case EPackageType.LoadBot:
                    writer.Put(BotProfileId);
                    break;
                case EPackageType.TrainSync:
                    writer.Put(DepartureTime);
                    break;
                case EPackageType.ExfilCountdown:
                    writer.Put(ExfilName);
                    writer.Put(ExfilStartTime);
                    break;
                case EPackageType.TraderServiceNotification:
                    writer.Put((int)TraderServiceType);
                    break;
                case EPackageType.DisposeBot:
                    writer.Put(BotProfileId);
                    break;
            }
        }
    }

    public enum EPackageType
    {
        ClientExtract,
        Ping,
        LoadBot,
        TrainSync,
        ExfilCountdown,
        TraderServiceNotification,
        DisposeBot
    }
}
