using Comfort.Common;
using EFT;
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

            public void HandleRequest(NetPeer peer = null)
            {
                if (Singleton<IFikaGame>.Instance is CoopGame coopGame)
                {
                    if (FikaBackendUtils.IsServer)
                    {
                        RequestPacket response = new()
                        {
                            PacketType = ERequestSubPacketType.SpawnPoint,
                            RequestSubPacket = new SpawnPointRequest(coopGame.SpawnPointName)
                        };

                        Singleton<FikaServer>.Instance.SendDataToPeer(peer, ref response, DeliveryMethod.ReliableUnordered);
                        return;
                    }
                }

                FikaPlugin.Instance.FikaLogger.LogError("SpawnPointRequest::HandleRequest: CoopGame was null upon receiving packet!");
            }

            public void HandleResponse()
            {
                if (Singleton<IFikaGame>.Instance is CoopGame coopGame)
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

            public void HandleRequest(NetPeer peer)
            {
                if (Singleton<IFikaGame>.Instance is CoopGame coopGame)
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

                    Singleton<FikaServer>.Instance.SendDataToPeer(peer, ref response, DeliveryMethod.ReliableOrdered);
                    return;
                }

                FikaPlugin.Instance.FikaLogger.LogError("WeatherRequest::HandleRequest: CoopGame was null upon receiving packet!");
            }

            public void HandleResponse()
            {
                if (Singleton<IFikaGame>.Instance is CoopGame coopGame)
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
            public Dictionary<string, EExfiltrationStatus> ExfiltrationPoints;
            public List<int> StartTimes;
            public Dictionary<string, EExfiltrationStatus> ScavExfiltrationPoints;
            public List<int> ScavStartTimes;

            public ExfiltrationRequest()
            {

            }

            public ExfiltrationRequest(NetDataReader reader)
            {
                int exfilAmount = reader.GetInt();
                ExfiltrationPoints = [];
                StartTimes = [];
                for (int i = 0; i < exfilAmount; i++)
                {
                    ExfiltrationPoints.Add(reader.GetString(), (EExfiltrationStatus)reader.GetByte());
                    StartTimes.Add(reader.GetInt());
                }
                if (reader.GetBool())
                {
                    int scavExfilAmount = reader.GetInt();
                    ScavExfiltrationPoints = [];
                    ScavStartTimes = [];
                    for (int i = 0; i < scavExfilAmount; i++)
                    {
                        ScavExfiltrationPoints.Add(reader.GetString(), (EExfiltrationStatus)reader.GetByte());
                        ScavStartTimes.Add(reader.GetInt());
                    }
                }
            }

            public void HandleRequest(NetPeer peer)
            {
                if (ExfiltrationControllerClass.Instance != null)
                {
                    FikaServer server = Singleton<FikaServer>.Instance;
                    ExfiltrationControllerClass exfilController = ExfiltrationControllerClass.Instance;

                    if (exfilController.ExfiltrationPoints == null)
                    {
                        return;
                    }

                    RequestPacket response = new()
                    {
                        PacketType = ERequestSubPacketType.Exfiltration
                    };

                    ExfiltrationRequest exfiltrationRequest = new()
                    {
                        ExfiltrationPoints = [],
                        StartTimes = []
                    };

                    foreach (ExfiltrationPoint exfilPoint in exfilController.ExfiltrationPoints)
                    {
                        exfiltrationRequest.ExfiltrationPoints.Add(exfilPoint.Settings.Name, exfilPoint.Status);
                        exfiltrationRequest.StartTimes.Add(exfilPoint.Settings.StartTime);
                    }

                    if (server.RaidSide == EPlayerSide.Savage && exfilController.ScavExfiltrationPoints != null)
                    {
                        exfiltrationRequest.ScavExfiltrationPoints = [];
                        exfiltrationRequest.ScavStartTimes = [];

                        foreach (ScavExfiltrationPoint scavExfilPoint in exfilController.ScavExfiltrationPoints)
                        {
                            exfiltrationRequest.ScavExfiltrationPoints.Add(scavExfilPoint.Settings.Name, scavExfilPoint.Status);
                            exfiltrationRequest.ScavStartTimes.Add(scavExfilPoint.Settings.StartTime);
                        }
                    }

                    response.RequestSubPacket = exfiltrationRequest;

                    server.SendDataToPeer(peer, ref response, DeliveryMethod.ReliableOrdered);
                    return;
                }

                FikaPlugin.Instance.FikaLogger.LogError($"ExfiltrationRequest::HandleRequest ExfiltrationController was null");
            }

            public void HandleResponse()
            {
                if (ExfiltrationControllerClass.Instance != null)
                {
                    ExfiltrationControllerClass exfilController = ExfiltrationControllerClass.Instance;

                    if (exfilController.ExfiltrationPoints == null)
                    {
                        return;
                    }

                    CoopGame coopGame = (CoopGame)Singleton<IFikaGame>.Instance;
                    if (coopGame == null)
                    {
#if DEBUG
                        FikaPlugin.Instance.FikaLogger.LogError("ExfiltrationRequest::HandleResponse coopGame was null!");
#endif
                        return;
                    }

                    int index = 0;
                    foreach (KeyValuePair<string, EExfiltrationStatus> exfilPoint in ExfiltrationPoints)
                    {
                        ExfiltrationPoint point = exfilController.ExfiltrationPoints.Where(x => x.Settings.Name == exfilPoint.Key).FirstOrDefault();
                        if (point != null || point != default)
                        {
                            point.Settings.StartTime = StartTimes[index];
                            index++;
                            if (point.Status != exfilPoint.Value && (exfilPoint.Value == EExfiltrationStatus.RegularMode || exfilPoint.Value == EExfiltrationStatus.UncompleteRequirements))
                            {
                                point.Enable();
                                point.Status = exfilPoint.Value;
                            }
                            else if (point.Status != exfilPoint.Value && exfilPoint.Value == EExfiltrationStatus.NotPresent || exfilPoint.Value == EExfiltrationStatus.Pending)
                            {
                                point.Disable();
                                point.Status = exfilPoint.Value;
                            }
                        }
                        else
                        {
                            FikaPlugin.Instance.FikaLogger.LogWarning($"ExfiltrationRequest::HandleResponse: Could not find exfil point with name '{exfilPoint.Key}'");
                        }
                    }

                    if (coopGame.RaidSettings.IsScav && exfilController.ScavExfiltrationPoints != null && ScavExfiltrationPoints != null)
                    {
                        string scavProfile = FikaGlobals.GetProfile(true).ProfileId;
                        int scavIndex = 0;
                        foreach (KeyValuePair<string, EExfiltrationStatus> scavExfilPoint in ScavExfiltrationPoints)
                        {
                            ScavExfiltrationPoint scavPoint = exfilController.ScavExfiltrationPoints.Where(x => x.Settings.Name == scavExfilPoint.Key).FirstOrDefault();
                            if (scavPoint != null || scavPoint != default)
                            {
                                scavPoint.Settings.StartTime = ScavStartTimes[scavIndex];
                                scavIndex++;
                                if (scavPoint.Status != scavExfilPoint.Value && scavExfilPoint.Value == EExfiltrationStatus.RegularMode)
                                {
                                    scavPoint.Enable();
                                    if (!string.IsNullOrEmpty(scavProfile))
                                    {
                                        scavPoint.EligibleIds.Add(scavProfile);
                                    }
                                    scavPoint.Status = scavExfilPoint.Value;
                                    coopGame.UpdateExfilPointFromServer(scavPoint, true);
                                }
                                else if (scavPoint.Status != scavExfilPoint.Value && (scavExfilPoint.Value == EExfiltrationStatus.NotPresent || scavExfilPoint.Value == EExfiltrationStatus.Pending))
                                {
                                    scavPoint.Disable();
                                    if (!string.IsNullOrEmpty(scavProfile))
                                    {
                                        scavPoint.EligibleIds.Remove(scavProfile);
                                    }
                                    scavPoint.Status = scavExfilPoint.Value;
                                    coopGame.UpdateExfilPointFromServer(scavPoint, false);
                                }
                            }
                            else
                            {
                                FikaPlugin.Instance.FikaLogger.LogWarning($"ExfiltrationRequest::HandleResponse: Could not find exfil point with name '{scavExfilPoint.Key}'");
                            }
                        }
                    }

                    coopGame.ExfiltrationReceived = true;
                    return;
                }

                FikaPlugin.Instance.FikaLogger.LogWarning($"ExfiltrationRequest::HandleResponse: ExfiltrationController was null");
            }

            public void Serialize(NetDataWriter writer)
            {
                int exfilAmount = ExfiltrationPoints.Count;
                writer.Put(exfilAmount);
                for (int i = 0; i < exfilAmount; i++)
                {
                    KeyValuePair<string, EExfiltrationStatus> kvp = ExfiltrationPoints.ElementAt(i);
                    writer.Put(kvp.Key);
                    writer.Put((byte)kvp.Value);
                    writer.Put(StartTimes[i]);
                }
                bool hasScavExfils = ScavExfiltrationPoints != null;
                writer.Put(hasScavExfils);
                if (hasScavExfils)
                {
                    int scavExfilAmount = ScavExfiltrationPoints.Count;
                    writer.Put(scavExfilAmount);
                    for (int i = 0; i < scavExfilAmount; i++)
                    {
                        KeyValuePair<string, EExfiltrationStatus> kvp = ScavExfiltrationPoints.ElementAt(i);
                        writer.Put(kvp.Key);
                        writer.Put((byte)kvp.Value);
                        writer.Put(ScavStartTimes[i]);
                    }
                }
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

            public void HandleRequest(NetPeer peer = null)
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

                    Singleton<FikaServer>.Instance.SendDataToPeer(peer, ref response, DeliveryMethod.ReliableOrdered);
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
                bool isRequest = string.IsNullOrEmpty(TraderId);
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
