using Comfort.Common;
using EFT.Interactive;
using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Fika.Core.Networking.SubPacket;

namespace Fika.Core.Networking
{
    public class RequestSubPackets
    {
        public class SpawnPointRequest : IRequestPacket
        {
            public string Name;

            public SpawnPointRequest(string name)
            {
                Name = name;
            }

            public SpawnPointRequest()
            {

            }

            public SpawnPointRequest(NetDataReader reader)
            {
                Name = reader.GetString();
            }

            public void HandleRequest(NetPeer peer, FikaServer server)
            {
                CoopGame coopGame = CoopGame.Instance;
                if (coopGame != null)
                {
                    if (FikaBackendUtils.IsServer)
                    {
                        RequestPacket response = new()
                        {
                            PacketType = ERequestSubPacketType.SpawnPoint,
                            RequestSubPacket = new SpawnPointRequest(coopGame.SpawnPointName)
                        };

                        server.SendDataToPeer(peer, ref response, DeliveryMethod.ReliableOrdered);
                        return;
                    }
                }

                FikaPlugin.Instance.FikaLogger.LogError("SpawnPointRequest::HandleRequest: CoopGame was null upon receiving packet!");
            }

            public void HandleResponse()
            {
                CoopGame coopGame = CoopGame.Instance;
                if (coopGame != null)
                {
                    if (!string.IsNullOrEmpty(Name))
                    {
                        coopGame.SpawnId = Name;
                        return;
                    }
                    FikaPlugin.Instance.FikaLogger.LogError("SpawnPointRequest::HandleResponse: Name was empty!");
                    return;
                }

                FikaPlugin.Instance.FikaLogger.LogError("SpawnPointRequest::HandleResponse: CoopGame was null upon receiving packet!");
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(Name);
            }
        }

        public class WeatherRequest : IRequestPacket
        {
            public ESeason Season;
            public Vector3 SpringSnowFactor;
            public WeatherClass[] WeatherClasses;

            public WeatherRequest()
            {

            }

            public WeatherRequest(NetDataReader reader)
            {
                Season = (ESeason)reader.GetByte();
                SpringSnowFactor = reader.GetVector3();
                int amount = reader.GetInt();
                WeatherClasses = new WeatherClass[amount];
                for (int i = 0; i < amount; i++)
                {
                    WeatherClasses[i] = reader.GetWeatherClass();
                }
            }

            public void HandleRequest(NetPeer peer, FikaServer server)
            {
                CoopGame coopGame = CoopGame.Instance;
                if (coopGame != null && coopGame.WeatherClasses != null && coopGame.WeatherClasses.Length > 0)
                {
                    RequestPacket response = new()
                    {
                        PacketType = ERequestSubPacketType.Weather,
                        RequestSubPacket = new WeatherRequest()
                        {
                            Season = coopGame.Season,
                            SpringSnowFactor = coopGame.SeasonsSettings != null ? coopGame.SeasonsSettings.SpringSnowFactor : Vector3.zero,
                            WeatherClasses = coopGame.WeatherClasses
                        }
                    };

                    server.SendDataToPeer(peer, ref response, DeliveryMethod.ReliableOrdered);
                }
            }

            public void HandleResponse()
            {
                CoopGame coopGame = CoopGame.Instance;
                if (coopGame != null)
                {
                    coopGame.Season = Season;
                    coopGame.SeasonsSettings = new()
                    {
                        SpringSnowFactor = SpringSnowFactor
                    };
                    coopGame.WeatherClasses = WeatherClasses;
                    return;
                }

                FikaPlugin.Instance.FikaLogger.LogError("WeatherRequest::HandleResponse: CoopGame was null upon receiving packet!");
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put((byte)Season);
                writer.Put(SpringSnowFactor);
                int amount = WeatherClasses.Length;
                writer.Put(amount);
                for (int i = 0; i < amount; i++)
                {
                    writer.PutWeatherClass(WeatherClasses[i]);
                }
            }
        }

        public class ExfiltrationRequest : IRequestPacket
        {
            public byte[] Data;

            public ExfiltrationRequest()
            {

            }

            public ExfiltrationRequest(NetDataReader reader)
            {
                Data = reader.GetByteArray();
            }

            public void HandleRequest(NetPeer peer, FikaServer server)
            {
                if (ExfiltrationControllerClass.Instance == null)
                {
                    FikaPlugin.Instance.FikaLogger.LogError("ExfiltrationRequest::HandleRequest: ExfiltrationControllerClass was null!");
                    return;
                }

                ExfiltrationControllerClass exfilController = ExfiltrationControllerClass.Instance;
                ExfiltrationPoint[] allExfils = exfilController.ExfiltrationPoints;

                FikaWriter writer = EFTSerializationManager.GetWriter();
                {
                    writer.WriteInt(allExfils.Length);
                    foreach (ExfiltrationPoint exfilPoint in allExfils)
                    {
                        writer.WriteString(exfilPoint.Settings.Name);
                        writer.WriteByte((byte)exfilPoint.Status);
                        writer.WriteInt(exfilPoint.Settings.StartTime);
                        if (exfilPoint.Status == EExfiltrationStatus.Countdown)
                        {
                            writer.WriteFloat(exfilPoint.ExfiltrationStartTime);
                        }
                    }

                    RequestPacket response = new()
                    {
                        PacketType = ERequestSubPacketType.Exfiltration,
                        RequestSubPacket = new ExfiltrationRequest()
                        {
                            Data = writer.ToArray()
                        }
                    };
                    server.SendDataToPeer(peer, ref response, DeliveryMethod.ReliableOrdered);
                }
            }

            public void HandleResponse()
            {
                if (Data == null || Data.Length == 0)
                {
                    FikaPlugin.Instance.FikaLogger.LogError("ExfiltrationRequest::HandleRequest: Data was null or empty!");
                    return;
                }

                if (ExfiltrationControllerClass.Instance == null)
                {
                    FikaPlugin.Instance.FikaLogger.LogError("ExfiltrationRequest::HandleRequest: ExfiltrationControllerClass was null!");
                    return;
                }

                ExfiltrationControllerClass exfilController = ExfiltrationControllerClass.Instance;
                ExfiltrationPoint[] allExfils = exfilController.ExfiltrationPoints;

                FikaReader eftReader = EFTSerializationManager.GetReader(Data);
                int amount = eftReader.ReadInt();
                for (int i = 0; i < amount; i++)
                {
                    string name = eftReader.ReadString();
                    EExfiltrationStatus status = (EExfiltrationStatus)eftReader.ReadByte();
                    int startTime = eftReader.ReadInt();
                    int exfilStartTime = -1;
                    if (status == EExfiltrationStatus.Countdown)
                    {
                        exfilStartTime = eftReader.ReadInt();
                    }

                    ExfiltrationPoint exfilPoint = allExfils.FirstOrDefault(x => x.Settings.Name == name);
                    if (exfilPoint != null)
                    {
                        exfilPoint.Status = status;
                        exfilPoint.Settings.StartTime = startTime;
                        if (exfilStartTime > 0)
                        {
                            exfilPoint.ExfiltrationStartTime = exfilStartTime;
                        }
                    }
                }

                CoopGame coopGame = CoopGame.Instance;
                if (coopGame != null)
                {
                    coopGame.ExfiltrationReceived = true;
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.PutByteArray(Data);
            }
        }

        public class TraderServicesRequest : IRequestPacket
        {
            public int NetId;
            public string TraderId;
            public List<TraderServicesClass> Services;

            public TraderServicesRequest()
            {

            }

            public TraderServicesRequest(NetDataReader reader)
            {
                NetId = reader.GetInt();
                bool isRequest = reader.GetBool();
                if (isRequest)
                {
                    TraderId = reader.GetString();
                    return;
                }

                Services = [];
                int amount = reader.GetInt();
                if (amount > 0)
                {
                    for (int i = 0; i < amount; i++)
                    {
                        Services.Add(reader.GetTraderService());
                    }
                }
            }

            public void HandleRequest(NetPeer peer, FikaServer server)
            {
                if (Singleton<IFikaNetworkManager>.Instance.CoopHandler.Players.TryGetValue(NetId, out CoopPlayer playerToApply))
                {
                    List<TraderServicesClass> services = playerToApply.GetAvailableTraderServices(TraderId).ToList();
                    RequestPacket response = new()
                    {
                        PacketType = ERequestSubPacketType.TraderServices,
                        RequestSubPacket = new TraderServicesRequest()
                        {
                            NetId = NetId,
                            Services = services
                        }
                    };

                    server.SendDataToPeer(peer, ref response, DeliveryMethod.ReliableOrdered);
                }
            }

            public void HandleResponse()
            {
                if (Services == null || Services.Count < 1)
                {
                    FikaPlugin.Instance.FikaLogger.LogWarning("OnTraderServicesPacketReceived: Services was 0, but might be intentional. Skipping...");
                    return;
                }

                if (Singleton<IFikaNetworkManager>.Instance.CoopHandler.Players.TryGetValue(NetId, out CoopPlayer playerToApply))
                {
                    playerToApply.method_160(Services);
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(NetId);
                bool isRequest = !string.IsNullOrEmpty(TraderId);
                writer.Put(isRequest);
                if (isRequest)
                {
                    writer.Put(TraderId);
                    return;
                }

                int amount = Services.Count;
                writer.Put(amount);
                if (amount > 0)
                {
                    for (int i = 0; i < Services.Count; i++)
                    {
                        writer.PutTraderService(Services[i]);
                    }
                }
            }
        }
    }
}
