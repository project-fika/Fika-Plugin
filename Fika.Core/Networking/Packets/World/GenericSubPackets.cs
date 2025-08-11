using Comfort.Common;
using EFT;
using EFT.AssetsManager;
using EFT.GlobalEvents;
using EFT.Interactive;
using EFT.Interactive.SecretExfiltrations;
using EFT.SynchronizableObjects;
using Fika.Core.Main.Components;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Pooling;
using Fika.Core.Utils;
using LiteNetLib.Utils;
using System.Collections.Generic;
using System.Linq;
using static Fika.Core.Networking.Packets.SubPacket;
using static Fika.Core.UI.FikaUIGlobals;

namespace Fika.Core.Networking.Packets.World
{
    public class GenericSubPackets
    {
        public class ClientExtract : IPoolSubPacket
        {
            public int NetId;

            private ClientExtract() { }

            public static ClientExtract CreateInstance()
            {
                return new ClientExtract();
            }

            public static ClientExtract FromValue(int netId)
            {
                ClientExtract packet = GenericSubPacketPoolManager.Instance.GetPacket<ClientExtract>(EGenericSubPacketType.ClientExtract);
                packet.NetId = netId;
                return packet;
            }

            public void Execute(FikaPlayer player = null)
            {
                CoopHandler coopHandler = Singleton<IFikaNetworkManager>.Instance.CoopHandler;
                if (coopHandler == null)
                {
                    FikaPlugin.Instance.FikaLogger.LogError("ClientExtract: CoopHandler was null!");
                    return;
                }

                if (coopHandler.Players.TryGetValue(NetId, out FikaPlayer playerToApply))
                {
                    coopHandler.Players.Remove(NetId);
                    coopHandler.HumanPlayers.Remove(playerToApply);
                    if (!coopHandler.ExtractedPlayers.Contains(NetId))
                    {
                        coopHandler.ExtractedPlayers.Add(NetId);
                        IFikaGame fikaGame = Singleton<IFikaGame>.Instance;
                        if (fikaGame != null)
                        {
                            fikaGame.ExtractedPlayers.Add(NetId);
                            if (FikaBackendUtils.IsServer)
                            {
                                (fikaGame.GameController as HostGameController).ClearHostAI(playerToApply);
                            }

                            if (FikaPlugin.ShowNotifications.Value)
                            {
                                string nickname = !string.IsNullOrEmpty(playerToApply.Profile.Info.MainProfileNickname) ? playerToApply.Profile.Info.MainProfileNickname : playerToApply.Profile.Nickname;
                                NotificationManagerClass.DisplayMessageNotification(string.Format(LocaleUtils.GROUP_MEMBER_EXTRACTED.Localized(),
                                    ColorizeText(EColor.GREEN, nickname)),
                                EFT.Communications.ENotificationDurationType.Default, EFT.Communications.ENotificationIconType.EntryPoint);
                            }
                        }
                    }

                    if (playerToApply != null)
                    {
                        playerToApply.Dispose();
                        AssetPoolObject.ReturnToPool(playerToApply.gameObject, true);
                    }
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(NetId);
            }

            public void Deserialize(NetDataReader reader)
            {
                NetId = reader.GetInt();
            }

            public void Dispose()
            {
                NetId = 0;
            }
        }


        public class ClientConnected : IPoolSubPacket
        {
            public string Name;

            private ClientConnected() { }

            public static ClientConnected CreateInstance()
            {
                return new ClientConnected();
            }

            public static ClientConnected FromValue(string name)
            {
                ClientConnected packet = GenericSubPacketPoolManager.Instance.GetPacket<ClientConnected>(EGenericSubPacketType.ClientConnected);
                packet.Name = name;
                return packet;
            }

            public void Execute(FikaPlayer player = null)
            {
                string message = string.Format(LocaleUtils.UI_PLAYER_CONNECTED.Localized(), ColorizeText(EColor.BLUE, Name));
                NotificationManagerClass.DisplayMessageNotification(message);
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(Name);
            }

            public void Deserialize(NetDataReader reader)
            {
                Name = reader.GetString();
            }

            public void Dispose()
            {
                Name = null;
            }
        }


        public class ClientDisconnected : IPoolSubPacket
        {
            public string Name;

            private ClientDisconnected() { }

            public static ClientDisconnected CreateInstance()
            {
                return new ClientDisconnected();
            }

            public static ClientDisconnected FromValue(string name)
            {
                ClientDisconnected packet = GenericSubPacketPoolManager.Instance.GetPacket<ClientDisconnected>(EGenericSubPacketType.ClientDisconnected);
                packet.Name = name;
                return packet;
            }

            public void Execute(FikaPlayer player = null)
            {
                string message = string.Format(LocaleUtils.UI_PLAYER_DISCONNECTED.Localized(), ColorizeText(EColor.BLUE, Name));
                NotificationManagerClass.DisplayMessageNotification(message);
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(Name);
            }

            public void Deserialize(NetDataReader reader)
            {
                Name = reader.GetString();
            }

            public void Dispose()
            {
                Name = null;
            }
        }


        public class ExfilCountdown : IPoolSubPacket
        {
            public string ExfilName;
            public float ExfilStartTime;

            private ExfilCountdown() { }

            public static ExfilCountdown CreateInstance()
            {
                return new ExfilCountdown();
            }

            public static ExfilCountdown FromValue(string exfilName, float exfilStartTime)
            {
                ExfilCountdown packet = GenericSubPacketPoolManager.Instance.GetPacket<ExfilCountdown>(EGenericSubPacketType.ExfilCountdown);
                packet.ExfilName = exfilName;
                packet.ExfilStartTime = exfilStartTime;
                return packet;
            }

            public void Execute(FikaPlayer player = null)
            {
                CoopHandler coopHandler = Singleton<IFikaNetworkManager>.Instance.CoopHandler;
                if (coopHandler == null)
                {
                    FikaPlugin.Instance.FikaLogger.LogError("ClientExtract: CoopHandler was null!");
                    return;
                }

                if (ExfiltrationControllerClass.Instance != null)
                {
                    IFikaGame fikaGame = Singleton<IFikaGame>.Instance;
                    if (fikaGame == null)
                    {
                        FikaGlobals.LogError("ExfilCountdown: FikaGame was null");
                        return;
                    }

                    ExfiltrationControllerClass exfilController = ExfiltrationControllerClass.Instance;
                    foreach (ExfiltrationPoint exfiltrationPoint in exfilController.ExfiltrationPoints)
                    {
                        if (exfiltrationPoint.Settings.Name == ExfilName)
                        {
                            exfiltrationPoint.ExfiltrationStartTime = fikaGame != null ? fikaGame.GameController.GameInstance.PastTime : ExfilStartTime;

                            if (exfiltrationPoint.Status != EExfiltrationStatus.Countdown)
                            {
                                exfiltrationPoint.Status = EExfiltrationStatus.Countdown;
                            }
                            return;
                        }
                    }

                    if (exfilController.SecretExfiltrationPoints != null)
                    {
                        foreach (SecretExfiltrationPoint secretExfiltration in exfilController.SecretExfiltrationPoints)
                        {
                            if (secretExfiltration.Settings.Name == ExfilName)
                            {
                                secretExfiltration.ExfiltrationStartTime = fikaGame != null ? fikaGame.GameController.GameInstance.PastTime : ExfilStartTime;

                                if (secretExfiltration.Status != EExfiltrationStatus.Countdown)
                                {
                                    secretExfiltration.Status = EExfiltrationStatus.Countdown;
                                }
                                return;
                            }
                        }
                    }

                    FikaPlugin.Instance.FikaLogger.LogError("ExfilCountdown: Could not find ExfiltrationPoint: " + ExfilName);
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(ExfilName);
                writer.Put(ExfilStartTime);
            }

            public void Deserialize(NetDataReader reader)
            {
                ExfilName = reader.GetString();
                ExfilStartTime = reader.GetFloat();
            }

            public void Dispose()
            {
                ExfilName = null;
                ExfilStartTime = 0f;
            }
        }


        public class ClearEffects : IPoolSubPacket
        {
            public int NetId;

            private ClearEffects() { }

            public static ClearEffects CreateInstance()
            {
                return new ClearEffects();
            }

            public static ClearEffects FromValue(int netId)
            {
                ClearEffects packet = GenericSubPacketPoolManager.Instance.GetPacket<ClearEffects>(EGenericSubPacketType.ClearEffects);
                packet.NetId = netId;
                return packet;
            }

            public void Execute(FikaPlayer player = null)
            {
                if (FikaBackendUtils.IsServer)
                {
                    return;
                }

                CoopHandler coopHandler = Singleton<IFikaNetworkManager>.Instance.CoopHandler;
                if (coopHandler == null)
                {
                    FikaPlugin.Instance.FikaLogger.LogError("ClientExtract: CoopHandler was null!");
                    return;
                }

                if (coopHandler.Players.TryGetValue(NetId, out FikaPlayer playerToApply))
                {
                    if (playerToApply is ObservedPlayer observedPlayer)
                    {
                        observedPlayer.HealthBar.ClearEffects();
                    }
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(NetId);
            }

            public void Deserialize(NetDataReader reader)
            {
                NetId = reader.GetInt();
            }

            public void Dispose()
            {
                NetId = 0;
            }
        }


        public class UpdateBackendData : IPoolSubPacket
        {
            public int PlayerAmount;

            private UpdateBackendData() { }

            public static UpdateBackendData CreateInstance()
            {
                return new();
            }

            public static UpdateBackendData FromValue(int playerAmount)
            {
                UpdateBackendData packet = GenericSubPacketPoolManager.Instance.GetPacket<UpdateBackendData>(EGenericSubPacketType.UpdateBackendData);
                packet.PlayerAmount = playerAmount;
                return packet;
            }

            public void Execute(FikaPlayer player = null)
            {
                Singleton<IFikaNetworkManager>.Instance.PlayerAmount = PlayerAmount;
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(PlayerAmount);
            }

            public void Deserialize(NetDataReader reader)
            {
                PlayerAmount = reader.GetInt();
            }

            public void Dispose()
            {
                PlayerAmount = 0;
            }
        }


        public class SecretExfilFound : IPoolSubPacket
        {
            public string GroupId;
            public string ExitName;

            private SecretExfilFound() { }

            public static SecretExfilFound CreateInstance()
            {
                return new SecretExfilFound();
            }

            public static SecretExfilFound FromValue(string groupId, string exitName)
            {
                SecretExfilFound packet = GenericSubPacketPoolManager.Instance.GetPacket<SecretExfilFound>(EGenericSubPacketType.SecretExfilFound);
                packet.GroupId = groupId;
                packet.ExitName = exitName;
                return packet;
            }

            public void Execute(FikaPlayer player = null)
            {
                GlobalEventHandlerClass.Instance
                    .CreateCommonEvent<SecretExfiltrationPointFoundShareEvent>()
                    .Invoke(GroupId, GroupId, ExitName);
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(GroupId);
                writer.Put(ExitName);
            }

            public void Deserialize(NetDataReader reader)
            {
                GroupId = reader.GetString();
                ExitName = reader.GetString();
            }

            public void Dispose()
            {
                GroupId = null;
                ExitName = null;
            }
        }


        public class BorderZoneEvent : IPoolSubPacket
        {
            public string ProfileId;
            public int ZoneId;

            private BorderZoneEvent() { }

            public static BorderZoneEvent CreateInstance()
            {
                return new BorderZoneEvent();
            }

            public static BorderZoneEvent FromValue(string profileId, int zoneId)
            {
                BorderZoneEvent packet = GenericSubPacketPoolManager.Instance.GetPacket<BorderZoneEvent>(EGenericSubPacketType.BorderZone);
                packet.ProfileId = profileId;
                packet.ZoneId = zoneId;
                return packet;
            }

            public void Execute(FikaPlayer player = null)
            {
                if (!Singleton<GameWorld>.Instantiated)
                {
                    return;
                }

                BorderZone[] borderZones = Singleton<GameWorld>.Instance.BorderZones;
                if (borderZones == null || borderZones.Length == 0)
                {
                    return;
                }

                foreach (BorderZone borderZone in borderZones)
                {
                    if (borderZone.Id == ZoneId)
                    {
                        List<IPlayer> players = Singleton<GameWorld>.Instance.RegisteredPlayers;
                        foreach (IPlayer iPlayer in players)
                        {
                            if (iPlayer.ProfileId == ProfileId)
                            {
                                IPlayerOwner playerBridge = Singleton<GameWorld>.Instance.GetAlivePlayerBridgeByProfileID(ProfileId);
                                if (playerBridge != null)
                                {
                                    borderZone.ProcessIncomingPacket(playerBridge);
                                }
                                break;
                            }
                        }
                    }
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(ProfileId);
                writer.Put(ZoneId);
            }

            public void Deserialize(NetDataReader reader)
            {
                ProfileId = reader.GetString();
                ZoneId = reader.GetInt();
            }

            public void Dispose()
            {
                ProfileId = null;
                ZoneId = 0;
            }
        }


        public class MineEvent : IPoolSubPacket
        {
            public Vector3 MinePosition;

            private MineEvent() { }

            public static MineEvent CreateInstance()
            {
                return new MineEvent();
            }

            public static MineEvent FromValue(Vector3 minePosition)
            {
                MineEvent packet = GenericSubPacketPoolManager.Instance.GetPacket<MineEvent>(EGenericSubPacketType.Mine);
                packet.MinePosition = minePosition;
                return packet;
            }

            public void Execute(FikaPlayer player = null)
            {
                if (Singleton<GameWorld>.Instance.MineManager != null)
                {
                    NetworkGame<EftGamePlayerOwner>.Class1557 mineSeeker = new()
                    {
                        minePosition = MinePosition
                    };
                    MineDirectional mineDirectional = Singleton<GameWorld>.Instance.MineManager.Mines.FirstOrDefault(mineSeeker.method_0);
                    if (mineDirectional == null)
                    {
                        return;
                    }
                    mineDirectional.Explosion();
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.PutUnmanaged(MinePosition);
            }

            public void Deserialize(NetDataReader reader)
            {
                MinePosition = reader.GetUnmanaged<Vector3>();
            }

            public void Dispose()
            {
                MinePosition = default;
            }
        }


        public class DisarmTripwire : IPoolSubPacket
        {
            public AirplaneDataPacketStruct Data;

            private DisarmTripwire() { }

            public static DisarmTripwire CreateInstance()
            {
                return new DisarmTripwire();
            }

            public static DisarmTripwire FromValue(AirplaneDataPacketStruct data)
            {
                DisarmTripwire packet = GenericSubPacketPoolManager.Instance.GetPacket<DisarmTripwire>(EGenericSubPacketType.DisarmTripwire);
                packet.Data = data;
                return packet;
            }

            public void Execute(FikaPlayer player = null)
            {
                if (Data.ObjectType == SynchronizableObjectType.Tripwire)
                {
                    GameWorld gameWorld = Singleton<GameWorld>.Instance;
                    TripwireSynchronizableObject tripwire = gameWorld.SynchronizableObjectLogicProcessor.TripwireManager.GetTripwireById(Data.ObjectId);
                    if (tripwire != null)
                    {
                        gameWorld.DeActivateTripwire(tripwire);
                        return;
                    }

                    FikaGlobals.LogError($"OnSyncObjectPacketReceived: Tripwire with id {Data.ObjectId} could not be found!");
                }

                FikaGlobals.LogWarning($"OnSyncObjectPacketReceived: Received a packet we shouldn't receive: {Data.ObjectType}");
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.PutAirplaneDataPacketStruct(Data);
            }

            public void Deserialize(NetDataReader reader)
            {
                Data = reader.GetAirplaneDataPacketStruct();
            }

            public void Dispose()
            {
                Data = default;
            }
        }


        public class MuffledState : IPoolSubPacket
        {
            public int NetId;
            public bool Muffled;

            private MuffledState() { }

            public static MuffledState CreateInstance()
            {
                return new MuffledState();
            }

            public static MuffledState FromValue(int netId, bool muffled)
            {
                MuffledState packet = GenericSubPacketPoolManager.Instance.GetPacket<MuffledState>(EGenericSubPacketType.MuffledState);
                packet.NetId = netId;
                packet.Muffled = muffled;
                return packet;
            }

            public void Execute(FikaPlayer player = null)
            {
                if (CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler))
                {
                    if (coopHandler.Players.TryGetValue(NetId, out FikaPlayer fikaPlayer) && fikaPlayer is ObservedPlayer observed)
                    {
                        observed.SetMuffledState(Muffled);
                        return;
                    }

                    FikaGlobals.LogError($"MuffledState: Could not find player with id {NetId} or they were not observed!");
                    return;
                }

                FikaGlobals.LogWarning($"MuffledState: Could not get CoopHandler!");
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(NetId);
                writer.Put(Muffled);
            }

            public void Deserialize(NetDataReader reader)
            {
                NetId = reader.GetInt();
                Muffled = reader.GetBool();
            }

            public void Dispose()
            {
                NetId = 0;
                Muffled = false;
            }
        }


        public class BtrSpawn : IPoolSubPacket
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public string PlayerProfileId;

            private BtrSpawn() { }

            public static BtrSpawn CreateInstance()
            {
                return new BtrSpawn();
            }

            public static BtrSpawn FromValue(Vector3 position, Quaternion rotation, string profileId)
            {
                BtrSpawn packet = GenericSubPacketPoolManager.Instance.GetPacket<BtrSpawn>(EGenericSubPacketType.SpawnBTR);
                packet.Position = position;
                packet.Rotation = rotation;
                packet.PlayerProfileId = profileId;
                return packet;
            }

            public void Execute(FikaPlayer player = null)
            {
                GlobalEventHandlerClass.CreateEvent<BtrSpawnOnThePathEvent>().Invoke(Position, Rotation, PlayerProfileId);
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.PutUnmanaged(Position);
                writer.PutUnmanaged(Rotation);
                writer.Put(PlayerProfileId);
            }

            public void Deserialize(NetDataReader reader)
            {
                Position = reader.GetUnmanaged<Vector3>();
                Rotation = reader.GetUnmanaged<Quaternion>();
                PlayerProfileId = reader.GetString();
            }

            public void Dispose()
            {
                Position = default;
                Rotation = default;
                PlayerProfileId = null;
            }
        }
    }
}
