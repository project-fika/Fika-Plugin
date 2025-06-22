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
            public string Infiltration;
            public Vector3 Position;
            public Quaternion Rotation;

            public SpawnPointRequest(string name, Vector3 position, Quaternion rotation)
            {
                Infiltration = name;
                Position = position;
                Rotation = rotation;
            }

            public SpawnPointRequest()
            {

            }

            public SpawnPointRequest(NetDataReader reader)
            {
                Infiltration = reader.GetString();
                Position = reader.GetVector3();
                Rotation = reader.GetQuaternion();
            }

            public void HandleRequest(NetPeer peer, FikaServer server)
            {
                IFikaGame fikaGame = Singleton<IFikaGame>.Instance;
                if (fikaGame != null)
                {
                    if (FikaBackendUtils.IsServer && !string.IsNullOrEmpty(fikaGame.GameController.InfiltrationPoint) && fikaGame.GameController.SpawnPoint != null)
                    {
                        RequestPacket response = new()
                        {
                            PacketType = ERequestSubPacketType.SpawnPoint,
                            RequestSubPacket = new SpawnPointRequest(fikaGame.GameController.InfiltrationPoint,
                            fikaGame.GameController.SpawnPoint.Position, fikaGame.GameController.SpawnPoint.Rotation)
                        };

                        server.SendDataToPeer(peer, ref response, DeliveryMethod.ReliableOrdered);
                        return;
                    }
                }

                FikaGlobals.LogError("SpawnPointRequest::HandleRequest: CoopGame was null upon receiving packet!");
            }

            public void HandleResponse()
            {
                IFikaGame fikaGame = Singleton<IFikaGame>.Instance;
                if (fikaGame != null)
                {
                    if (!string.IsNullOrEmpty(Infiltration))
                    {
                        fikaGame.GameController.InfiltrationPoint = Infiltration;
                        fikaGame.GameController.ClientSpawnPosition = Position;
                        fikaGame.GameController.ClientSpawnRotation = Rotation;
                        FikaGlobals.LogInfo($"Received spawn position from host: {Position}, rotation: {Rotation}");
                        return;
                    }
                    FikaGlobals.LogError("SpawnPointRequest::HandleResponse: Infiltration was empty!");
                    return;
                }

                FikaGlobals.LogError("SpawnPointRequest::HandleResponse: CoopGame was null upon receiving packet!");
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(Infiltration);
                writer.PutVector3(Position);
                writer.PutQuaternion(Rotation);
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
                IFikaGame fikaGame = Singleton<IFikaGame>.Instance;
                if (fikaGame != null && fikaGame.GameController.WeatherClasses != null && fikaGame.GameController.WeatherClasses.Length > 0)
                {
                    RequestPacket response = new()
                    {
                        PacketType = ERequestSubPacketType.Weather,
                        RequestSubPacket = new WeatherRequest()
                        {
                            Season = fikaGame.Season,
                            SpringSnowFactor = fikaGame.SeasonsSettings != null ? fikaGame.SeasonsSettings.SpringSnowFactor : Vector3.zero,
                            WeatherClasses = fikaGame.GameController.WeatherClasses
                        }
                    };

                    server.SendDataToPeer(peer, ref response, DeliveryMethod.ReliableOrdered);
                }
            }

            public void HandleResponse()
            {
                IFikaGame fikaGame = Singleton<IFikaGame>.Instance;
                if (fikaGame != null)
                {
                    fikaGame.Season = Season;
                    if (SpringSnowFactor != Vector3.zero)
                    {
                        fikaGame.SeasonsSettings = new()
                        {
                            SpringSnowFactor = SpringSnowFactor
                        };
                    }
                    fikaGame.GameController.WeatherClasses = WeatherClasses;
                    return;
                }

                FikaGlobals.LogError("WeatherRequest::HandleResponse: CoopGame was null upon receiving packet!");
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put((byte)Season);
                writer.PutVector3(SpringSnowFactor);
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
                    FikaGlobals.LogError("ExfiltrationRequest::HandleRequest: ExfiltrationControllerClass was null!");
                    return;
                }

                ExfiltrationControllerClass exfilController = ExfiltrationControllerClass.Instance;
                ExfiltrationPoint[] allExfils = exfilController.ExfiltrationPoints;

                NetDataWriter writer = new();
                writer.Put(allExfils.Length);
                foreach (ExfiltrationPoint exfilPoint in allExfils)
                {
                    writer.Put(exfilPoint.Settings.Name);
                    writer.Put((byte)exfilPoint.Status);
                    writer.Put(exfilPoint.Settings.StartTime);
                    if (exfilPoint.Status == EExfiltrationStatus.Countdown)
                    {
                        writer.Put(exfilPoint.ExfiltrationStartTime);
                    }
                }

                RequestPacket response = new()
                {
                    PacketType = ERequestSubPacketType.Exfiltration,
                    RequestSubPacket = new ExfiltrationRequest()
                    {
                        Data = writer.Data
                    }
                };
                server.SendDataToPeer(peer, ref response, DeliveryMethod.ReliableOrdered);
            }

            public void HandleResponse()
            {
                if (Data == null || Data.Length == 0)
                {
                    FikaGlobals.LogError("ExfiltrationRequest::HandleRequest: Data was null or empty!");
                    return;
                }

                if (ExfiltrationControllerClass.Instance == null)
                {
                    FikaGlobals.LogError("ExfiltrationRequest::HandleRequest: ExfiltrationControllerClass was null!");
                    return;
                }

                ExfiltrationControllerClass exfilController = ExfiltrationControllerClass.Instance;
                ExfiltrationPoint[] allExfils = exfilController.ExfiltrationPoints;

                NetDataReader reader = new(Data);
                int amount = reader.GetInt();
                for (int i = 0; i < amount; i++)
                {
                    string name = reader.GetString();
                    EExfiltrationStatus status = (EExfiltrationStatus)reader.GetByte();
                    int startTime = reader.GetInt();
                    int exfilStartTime = -1;
                    if (status == EExfiltrationStatus.Countdown)
                    {
                        exfilStartTime = reader.GetInt();
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

                IFikaGame fikaGame = Singleton<IFikaGame>.Instance;
                if (fikaGame != null)
                {
                    (fikaGame.GameController as ClientGameController).ExfiltrationReceived = true;
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
                    FikaGlobals.LogWarning("OnTraderServicesPacketReceived: Services was 0, but might be intentional. Skipping...");
                    return;
                }

                if (Singleton<IFikaNetworkManager>.Instance.CoopHandler.Players.TryGetValue(NetId, out CoopPlayer playerToApply))
                {
                    playerToApply.method_164(Services);
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

        public class RequestCharactersPacket : IRequestPacket
        {
            public List<int> MissingIds;

            public RequestCharactersPacket(List<int> missingIds)
            {
#if DEBUG
                FikaGlobals.LogWarning($"Requesting {missingIds.Count} missing ids");
#endif
                MissingIds = [.. missingIds];
            }

            public RequestCharactersPacket(NetDataReader reader)
            {
                int amount = reader.GetUShort();
#if DEBUG
                FikaGlobals.LogWarning($"A client has requested {amount} missing players");
#endif
                if (amount > 0)
                {
                    MissingIds = new(amount);
                    for (int i = 0; i < amount; i++)
                    {
                        MissingIds.Add(reader.GetInt());
                    }
                }
            }

            public void HandleRequest(NetPeer peer, FikaServer server)
            {
                if (MissingIds != null && server.CoopHandler != null)
                {
                    foreach (int netId in MissingIds)
                    {
#if DEBUG
                        FikaGlobals.LogWarning($"Looking for missing netId {netId}");
#endif
                        if (server.CoopHandler.Players.TryGetValue(netId, out CoopPlayer coopPlayer))
                        {
#if DEBUG
                            FikaGlobals.LogWarning($"Found {coopPlayer.Profile.Nickname} that was missing from client, sending...");
#endif
                            SendCharacterPacket packet = new(new()
                            {
                                Profile = coopPlayer.Profile,
                                ControllerId = coopPlayer.InventoryController.CurrentId,
                                FirstOperationId = coopPlayer.InventoryController.NextOperationId
                            }, coopPlayer.HealthController.IsAlive, coopPlayer.IsAI, coopPlayer.Transform.position, coopPlayer.NetId);

                            if (coopPlayer.ActiveHealthController != null)
                            {
                                packet.PlayerInfoPacket.HealthByteArray = coopPlayer.ActiveHealthController.SerializeState();
                            }
                            else
                            {
                                packet.PlayerInfoPacket.HealthByteArray = coopPlayer.Profile.Health.SerializeHealthInfo();
                            }

                            if (coopPlayer.HandsController != null)
                            {
                                packet.PlayerInfoPacket.ControllerType = HandsControllerToEnumClass.FromController(coopPlayer.HandsController);
                                packet.PlayerInfoPacket.ItemId = coopPlayer.HandsController.Item.Id;
                                packet.PlayerInfoPacket.IsStationary = coopPlayer.MovementContext.IsStationaryWeaponInHands;
                            }

                            server.SendDataToPeer(peer, ref packet, DeliveryMethod.ReliableOrdered);
                        }
                    }
                }
            }

            /// <summary>
            /// Handled in <see cref="FikaClient.OnSendCharacterPacketReceived(SendCharacterPacket)"/>
            /// </summary>
            public void HandleResponse()
            {
                // Do nothing
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put((ushort)MissingIds.Count);
                if (MissingIds.Count > 0)
                {
                    foreach (int netId in MissingIds)
                    {
                        writer.Put(netId);
                    }
                }
            }
        }
    }
}
