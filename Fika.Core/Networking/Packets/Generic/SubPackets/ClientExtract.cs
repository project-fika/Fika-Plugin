using Comfort.Common;
using EFT.AssetsManager;
using Fika.Core.Main.Components;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Pooling;
using static Fika.Core.UI.FikaUIGlobals;

namespace Fika.Core.Networking.Packets.Generic.SubPackets;

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
