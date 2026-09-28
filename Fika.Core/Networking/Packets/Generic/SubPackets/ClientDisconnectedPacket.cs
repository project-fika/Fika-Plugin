using EFT;
using EFT.Communications;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using static Fika.Core.UI.FikaUIGlobals;

namespace Fika.Core.Networking.Packets.Generic.SubPackets;

public readonly struct ClientDisconnectedPacket : IGenericPacket
{
    public EGenericPacketType Type => EGenericPacketType.ClientDisconnected;

    public ClientDisconnectedPacket(string name)
    {
        Name = name;
    }

    public ClientDisconnectedPacket(NetDataReader reader)
    {
        Name = reader.GetString();
    }

    public readonly string Name;

    public readonly void Execute()
    {
        var message = string.Format(LocaleUtils.UI_PLAYER_DISCONNECTED.Localized(), ColorizeText(EColor.BLUE, Name));
        NotificationManager.DisplayMessageNotification(message);
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(Name);
    }
}
