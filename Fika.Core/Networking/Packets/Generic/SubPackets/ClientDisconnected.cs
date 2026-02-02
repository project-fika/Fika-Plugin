using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Pooling;
using static Fika.Core.UI.FikaUIGlobals;

namespace Fika.Core.Networking.Packets.Generic.SubPackets;

public sealed class ClientDisconnected : IPoolSubPacket
{
    public string Name;

    private ClientDisconnected() { }

    public static ClientDisconnected CreateInstance()
    {
        return new ClientDisconnected();
    }

    public static ClientDisconnected FromValue(string name)
    {
        var packet = GenericSubPacketPoolManager.Instance.GetPacket<ClientDisconnected>(EGenericSubPacketType.ClientDisconnected);
        packet.Name = name;
        return packet;
    }

    public void Execute(FikaPlayer player = null)
    {
        var message = string.Format(LocaleUtils.UI_PLAYER_DISCONNECTED.Localized(), ColorizeText(EColor.BLUE, Name));
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
