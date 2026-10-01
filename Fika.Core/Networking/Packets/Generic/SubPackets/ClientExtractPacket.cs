using Comfort.Common;
using EFT;
using EFT.AssetsManager;
using EFT.Communications;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.Utils;
using static Fika.Core.UI.FikaUIGlobals;

namespace Fika.Core.Networking.Packets.Generic.SubPackets;

public readonly struct ClientExtractPacket(int netId) : IGenericPacket
{
    public EGenericPacketType Type => EGenericPacketType.ClientExtract;

    public readonly int NetId = netId;

    public readonly void Execute()
    {
        var coopHandler = Singleton<IFikaNetworkManager>.Instance.CoopHandler;
        if (coopHandler == null)
        {
            FikaGlobals.LogWarning("ClientExtract: CoopHandler was null! This is probably harmless.");
            return;
        }

        if (coopHandler.Players.TryGetValue(NetId, out var playerToApply))
        {
            coopHandler.Players.Remove(NetId);
            coopHandler.HumanPlayers.Remove(playerToApply);
            if (!coopHandler.ExtractedPlayers.Contains(NetId))
            {
                coopHandler.ExtractedPlayers.Add(NetId);
                var fikaGame = Singleton<IFikaGame>.Instance;
                if (fikaGame != null)
                {
                    fikaGame.ExtractedPlayers.Add(NetId);
                    if (FikaBackendUtils.IsServer)
                    {
                        (fikaGame.GameController as HostGameController).ClearHostAI(playerToApply);
                    }

                    if (FikaPlugin.Instance.Settings.ShowNotifications.Value)
                    {
                        var nickname = !string.IsNullOrEmpty(playerToApply.Profile.Info.MainProfileNickname) ? playerToApply.Profile.Info.MainProfileNickname : playerToApply.Profile.Nickname;
                        NotificationManager.DisplayMessageNotification(string.Format(LocaleUtils.GROUP_MEMBER_EXTRACTED.Localized(),
                            ColorizeText(EColor.GREEN, nickname)),
                        ENotificationDurationType.Default, ENotificationIconType.EntryPoint);
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

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
    }
}
