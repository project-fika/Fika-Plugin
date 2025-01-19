using Comfort.Common;
using EFT.AssetsManager;
using EFT.Interactive;
using EFT.Interactive.SecretExfiltrations;
using Fika.Core.Coop.Components;
using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using Fika.Core.Utils;
using LiteNetLib.Utils;
using static Fika.Core.Networking.Packets.SubPacket;
using static Fika.Core.UI.FikaUIGlobals;

namespace Fika.Core.Networking.Packets.GameWorld
{
    public class GenericSubPackets
    {
        public class ClientExtract : ISubPacket
        {
            public int NetId;

            public ClientExtract(int netId)
            {
                NetId = netId;
            }

            public void Execute(CoopPlayer player)
            {
                CoopHandler coopHandler = Singleton<IFikaNetworkManager>.Instance.CoopHandler;
                if (coopHandler == null)
                {
                    FikaPlugin.Instance.FikaLogger.LogError("ClientExtract: CoopHandler was null!");
                    return;
                }

                if (coopHandler.Players.TryGetValue(NetId, out CoopPlayer playerToApply))
                {
                    coopHandler.Players.Remove(NetId);
                    coopHandler.HumanPlayers.Remove(playerToApply);
                    if (!coopHandler.ExtractedPlayers.Contains(NetId))
                    {
                        coopHandler.ExtractedPlayers.Add(NetId);
                        CoopGame coopGame = coopHandler.LocalGameInstance;
                        if (coopGame != null)
                        {
                            coopGame.ExtractedPlayers.Add(NetId);
                            coopGame.ClearHostAI(playerToApply);

                            if (FikaPlugin.ShowNotifications.Value)
                            {
                                string nickname = !string.IsNullOrEmpty(playerToApply.Profile.Info.MainProfileNickname) ? playerToApply.Profile.Info.MainProfileNickname : playerToApply.Profile.Nickname;
                                NotificationManagerClass.DisplayMessageNotification(string.Format(LocaleUtils.GROUP_MEMBER_EXTRACTED.Localized(),
                                    ColorizeText(EColor.GREEN, nickname)),
                                EFT.Communications.ENotificationDurationType.Default, EFT.Communications.ENotificationIconType.EntryPoint);
                            }
                        }
                    }

                    playerToApply.Dispose();
                    AssetPoolObject.ReturnToPool(playerToApply.gameObject, true);
                }
            }

            public void Serialize(NetDataWriter writer)
            {

            }
        }

        public class ClientConnected : ISubPacket
        {
            public string Name;

            public ClientConnected(string name)
            {
                Name = name;
            }

            public ClientConnected(NetDataReader reader)
            {
                Name = reader.GetString();
            }

            public void Execute(CoopPlayer player)
            {
                string message = string.Format(LocaleUtils.UI_PLAYER_CONNECTED.Localized(), ColorizeText(EColor.BLUE, Name));
                NotificationManagerClass.DisplayMessageNotification(message);
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(Name);
            }
        }

        public class ClientDisconnected : ISubPacket
        {
            public string Name;

            public ClientDisconnected(string name)
            {
                Name = name;
            }

            public ClientDisconnected(NetDataReader reader)
            {
                Name = reader.GetString();
            }

            public void Execute(CoopPlayer player)
            {
                string message = string.Format(LocaleUtils.UI_PLAYER_DISCONNECTED.Localized(), ColorizeText(EColor.BLUE, Name));
                NotificationManagerClass.DisplayMessageNotification(message);
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(Name);
            }
        }

        public class ExfilCountdown : ISubPacket
        {
            public string ExfilName;
            public float ExfilStartTime;

            public ExfilCountdown(string exfilName, float exfilStartTime)
            {
                ExfilName = exfilName;
                ExfilStartTime = exfilStartTime;
            }

            public ExfilCountdown(NetDataReader reader)
            {
                ExfilName = reader.GetString();
                ExfilStartTime = reader.GetFloat();
            }

            public void Execute(CoopPlayer player)
            {
                CoopHandler coopHandler = Singleton<IFikaNetworkManager>.Instance.CoopHandler;
                if (coopHandler == null)
                {
                    FikaPlugin.Instance.FikaLogger.LogError("ClientExtract: CoopHandler was null!");
                    return;
                }

                if (ExfiltrationControllerClass.Instance != null)
                {
                    ExfiltrationControllerClass exfilController = ExfiltrationControllerClass.Instance;

                    foreach (ExfiltrationPoint exfiltrationPoint in exfilController.ExfiltrationPoints)
                    {
                        if (exfiltrationPoint.Settings.Name == ExfilName)
                        {
                            CoopGame game = coopHandler.LocalGameInstance;
                            exfiltrationPoint.ExfiltrationStartTime = game != null ? game.PastTime : ExfilStartTime;

                            if (exfiltrationPoint.Status != EExfiltrationStatus.Countdown)
                            {
                                exfiltrationPoint.Status = EExfiltrationStatus.Countdown;
                            }
                            return;
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
        }

        public class ClearEffects : ISubPacket
        {
            public int NetId;

            public ClearEffects(int netId)
            {
                NetId = netId;
            }

            public void Execute(CoopPlayer player)
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

                if (coopHandler.Players.TryGetValue(NetId, out CoopPlayer playerToApply))
                {
                    if (playerToApply is ObservedCoopPlayer observedPlayer)
                    {
                        observedPlayer.HealthBar.ClearEffects();
                    }
                }
            }

            public void Serialize(NetDataWriter writer)
            {

            }
        }

        public class UpdateBackendData : ISubPacket
        {
            public int ExpectedPlayers;

            public UpdateBackendData(int expectedPlayers)
            {
                ExpectedPlayers = expectedPlayers;
            }

            public UpdateBackendData(NetDataReader reader)
            {
                ExpectedPlayers = reader.GetInt();
            }

            public void Execute(CoopPlayer player)
            {
                FikaBackendUtils.HostExpectedNumberOfPlayers = ExpectedPlayers;
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(ExpectedPlayers);
            }
        }

        public class SecretExfilFound : ISubPacket
        {
            public string GroupId;
            public string ExitName;

            public SecretExfilFound(string groupId, string exitName)
            {
                GroupId = groupId;
                ExitName = exitName;
            }

            public SecretExfilFound(NetDataReader reader)
            {
                GroupId = reader.GetString();
                ExitName = reader.GetString();
            }

            public void Execute(CoopPlayer player)
            {
                GlobalEventHandlerClass.Instance.CreateCommonEvent<SecretExfiltrationPointFoundShareEvent>().Invoke(GroupId, GroupId, ExitName);
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(GroupId);
                writer.Put(ExitName);
            }
        }
    }
}
