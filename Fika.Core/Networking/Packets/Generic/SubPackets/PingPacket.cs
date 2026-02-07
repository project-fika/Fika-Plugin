using Fika.Core.Main.Factories;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.Generic.SubPackets;

public sealed class PingPacket : IPoolSubPacket
{
    private PingPacket() { }

    public static PingPacket CreateInstance()
    {
        return new();
    }

    public static PingPacket FromValue(Vector3 location, PingFactory.EPingType type, Color color, string nickname, string localeId = null)
    {
        var packet = GenericSubPacketPoolManager.Instance.GetPacket<PingPacket>(EGenericSubPacketType.Ping);
        packet.PingLocation = location;
        packet.PingType = type;
        packet.PingColor = color;
        packet.Nickname = nickname;
        packet.LocaleId = localeId;
        return packet;
    }

    public Vector3 PingLocation;
    public PingFactory.EPingType PingType;
    public Color PingColor;
    public string Nickname;
    public string LocaleId;

    public void Execute(FikaPlayer player = null)
    {
        if (FikaPlugin.Instance.Settings.UsePingSystem.Value && !FikaBackendUtils.IsHeadless)
        {
            PingFactory.ReceivePing(PingLocation, PingType, PingColor, Nickname, LocaleId);
        }
    }

    public void Deserialize(NetDataReader reader)
    {
        PingLocation = reader.GetUnmanaged<Vector3>();
        PingType = reader.GetEnum<PingFactory.EPingType>();
        PingColor = reader.GetUnmanaged<Color>();
        Nickname = reader.GetString();
        LocaleId = reader.GetString();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.PutUnmanaged(PingLocation);
        writer.PutEnum(PingType);
        writer.PutUnmanaged(PingColor);
        writer.Put(Nickname);
        writer.Put(LocaleId);
    }

    public void Dispose()
    {
        PingLocation = default;
        PingType = default;
        PingColor = default;
        Nickname = null;
        LocaleId = null;
    }
}
