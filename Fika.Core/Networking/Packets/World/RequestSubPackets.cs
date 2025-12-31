using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT.Interactive;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Packets.Generic;
using Fika.Core.Networking.Packets.Generic.SubPackets;

namespace Fika.Core.Networking.Packets.World;

public static class RequestSubPackets
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
            Position = reader.GetUnmanaged<Vector3>();
            Rotation = reader.GetUnmanaged<Quaternion>();
        }

        public void HandleRequest(NetPeer peer, FikaServer server)
        {
            var fikaGame = Singleton<IFikaGame>.Instance;
            if (fikaGame != null)
            {
                if (FikaBackendUtils.IsServer && !string.IsNullOrEmpty(fikaGame.GameController.InfiltrationPoint) && fikaGame.GameController.SpawnPoint != null)
                {
                    RequestPacket response = new()
                    {
                        Type = ERequestSubPacketType.SpawnPoint,
                        RequestSubPacket = new SpawnPointRequest(fikaGame.GameController.InfiltrationPoint,
                        fikaGame.GameController.SpawnPoint.Position, fikaGame.GameController.SpawnPoint.Rotation)
                    };

                    server.SendDataToPeer(ref response, DeliveryMethod.ReliableOrdered, peer);
                    return;
                }
            }

            FikaGlobals.LogError("SpawnPointRequest::HandleRequest: CoopGame was null upon receiving packet!");
        }

        public void HandleResponse()
        {
            var fikaGame = Singleton<IFikaGame>.Instance;
            if (fikaGame != null)
            {
                if (!string.IsNullOrEmpty(Infiltration))
                {
                    fikaGame.GameController.InfiltrationPoint = Infiltration;
                    fikaGame.GameController.ClientSpawnPosition = Position;
                    fikaGame.GameController.ClientSpawnRotation = Rotation;
                    FikaGlobals.LogInfo($"Received spawn position from host: {Position:F2}, rotation: {Rotation:F2}");
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
            writer.PutUnmanaged(Position);
            writer.PutUnmanaged(Rotation);
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
            Season = reader.GetEnum<ESeason>();
            SpringSnowFactor = reader.GetUnmanaged<Vector3>();
            var amount = reader.GetInt();
            WeatherClasses = new WeatherClass[amount];
            for (var i = 0; i < amount; i++)
            {
                WeatherClasses[i] = reader.GetWeatherClass();
            }
        }

        public void HandleRequest(NetPeer peer, FikaServer server)
        {
            var fikaGame = Singleton<IFikaGame>.Instance;
            if (fikaGame != null && fikaGame.GameController.WeatherClasses != null && fikaGame.GameController.WeatherClasses.Length > 0)
            {
                RequestPacket response = new()
                {
                    Type = ERequestSubPacketType.Weather,
                    RequestSubPacket = new WeatherRequest()
                    {
                        Season = fikaGame.Season,
                        SpringSnowFactor = fikaGame.SeasonsSettings != null ? fikaGame.SeasonsSettings.SpringSnowFactor : Vector3.zero,
                        WeatherClasses = fikaGame.GameController.WeatherClasses
                    }
                };

                server.SendDataToPeer(ref response, DeliveryMethod.ReliableOrdered, peer);
            }
        }

        public void HandleResponse()
        {
            var fikaGame = Singleton<IFikaGame>.Instance;
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
            writer.PutEnum(Season);
            writer.PutUnmanaged(SpringSnowFactor);
            var amount = WeatherClasses.Length;
            writer.Put(amount);
            for (var i = 0; i < amount; i++)
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

            var exfilController = ExfiltrationControllerClass.Instance;
            var allExfils = exfilController.ExfiltrationPoints;

            NetDataWriter writer = new();
            writer.Put(allExfils.Length);
            foreach (var exfilPoint in allExfils)
            {
                writer.Put(exfilPoint.Settings.Name);
                writer.PutEnum(exfilPoint.Status);
                writer.Put(exfilPoint.Settings.StartTime);
                if (exfilPoint.Status == EExfiltrationStatus.Countdown)
                {
                    writer.Put(exfilPoint.ExfiltrationStartTime);
                }
            }

            RequestPacket response = new()
            {
                Type = ERequestSubPacketType.Exfiltration,
                RequestSubPacket = new ExfiltrationRequest()
                {
                    Data = writer.Data
                }
            };
            server.SendDataToPeer(ref response, DeliveryMethod.ReliableOrdered, peer);
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

            var exfilController = ExfiltrationControllerClass.Instance;
            var allExfils = exfilController.ExfiltrationPoints;

            NetDataReader reader = new(Data);
            var amount = reader.GetInt();
            FikaGlobals.LogInfo($"Received {amount} exfils from the server");
            for (var i = 0; i < amount; i++)
            {
                var name = reader.GetString();
                var status = reader.GetEnum<EExfiltrationStatus>();
                var startTime = reader.GetInt();
                var exfilStartTime = -1;
                if (status == EExfiltrationStatus.Countdown)
                {
                    exfilStartTime = reader.GetInt();
                }

                var exfilPoint = allExfils.FirstOrDefault(x => x.Settings.Name == name);
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

            var fikaGame = Singleton<IFikaGame>.Instance;
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
            var isRequest = reader.GetBool();
            if (isRequest)
            {
                TraderId = reader.GetString();
                return;
            }

            Services = [];
            var amount = reader.GetInt();
            if (amount > 0)
            {
                for (var i = 0; i < amount; i++)
                {
                    Services.Add(reader.GetTraderService());
                }
            }
        }

        public void HandleRequest(NetPeer peer, FikaServer server)
        {
            if (Singleton<IFikaNetworkManager>.Instance.CoopHandler.Players.TryGetValue(NetId, out var playerToApply))
            {
                var services = playerToApply.GetAvailableTraderServices(TraderId).ToList();
                RequestPacket response = new()
                {
                    Type = ERequestSubPacketType.TraderServices,
                    RequestSubPacket = new TraderServicesRequest()
                    {
                        NetId = NetId,
                        Services = services
                    }
                };

                server.SendDataToPeer(ref response, DeliveryMethod.ReliableOrdered, peer);
            }
        }

        public void HandleResponse()
        {
            if (Services == null || Services.Count < 1)
            {
                FikaGlobals.LogWarning("OnTraderServicesPacketReceived: Services was 0, but might be intentional. Skipping...");
                return;
            }

            if (Singleton<IFikaNetworkManager>.Instance.CoopHandler.Players.TryGetValue(NetId, out var playerToApply))
            {
                playerToApply.method_166(Services);
            }
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(NetId);
            var isRequest = !string.IsNullOrEmpty(TraderId);
            writer.Put(isRequest);
            if (isRequest)
            {
                writer.Put(TraderId);
                return;
            }

            var amount = Services.Count;
            writer.Put(amount);
            if (amount > 0)
            {
                for (var i = 0; i < Services.Count; i++)
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
                for (var i = 0; i < amount; i++)
                {
                    MissingIds.Add(reader.GetInt());
                }
            }
        }

        public void HandleRequest(NetPeer peer, FikaServer server)
        {
            if (MissingIds != null && server.CoopHandler != null)
            {
                foreach (var netId in MissingIds)
                {
#if DEBUG
                    FikaGlobals.LogWarning($"Looking for missing netId {netId}");
#endif
                    if (server.CoopHandler.Players.TryGetValue(netId, out var fikaPlayer))
                    {
#if DEBUG
                        FikaGlobals.LogWarning($"Found {fikaPlayer.Profile.Nickname} that was missing from client, sending...");
#endif
                        var packet = SendCharacterPacket.FromValue(new()
                        {
                            Profile = fikaPlayer.Profile,
                            ControllerId = fikaPlayer.InventoryController.CurrentId,
                            FirstOperationId = fikaPlayer.InventoryController.NextOperationId
                        }, fikaPlayer.HealthController.IsAlive, fikaPlayer.IsAI, fikaPlayer.Transform.position, fikaPlayer.NetId);

                        if (fikaPlayer.ActiveHealthController != null)
                        {
                            packet.PlayerInfoPacket.HealthByteArray = fikaPlayer.ActiveHealthController.SerializeState();
                        }
                        else
                        {
                            packet.PlayerInfoPacket.HealthByteArray = fikaPlayer.Profile.Health.SerializeHealthInfo();
                        }

                        if (fikaPlayer.HandsController != null)
                        {
                            packet.PlayerInfoPacket.ControllerType = HandsControllerToEnumClass.FromController(fikaPlayer.HandsController);
                            packet.PlayerInfoPacket.ItemId = fikaPlayer.HandsController.Item.Id;
                            packet.PlayerInfoPacket.IsStationary = fikaPlayer.MovementContext.IsStationaryWeaponInHands;
                        }

                        server.SendGenericPacketToPeer(EGenericSubPacketType.SendCharacter, packet, peer);
                    }
                }
            }
        }

        /// <summary>
        /// Handled in <see cref="SendCharacterPacket.Execute(FikaPlayer)"/>
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
                foreach (var netId in MissingIds)
                {
                    writer.Put(netId);
                }
            }
        }
    }
}
