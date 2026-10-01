using Fika.Core.Main.Factories;
using Fika.Core.Main.Utils;

namespace Fika.Core.Networking.Packets.Generic.SubPackets;

public readonly struct PingPacket : IGenericPacket
{
    public EGenericPacketType Type => EGenericPacketType.Ping;

    public PingPacket(Vector3 location, PingFactory.EPingType type, Color color, string nickname, string localeId = null)
    {
        PingLocation = location;
        PingType = type;
        PingColor = color;
        Nickname = nickname;
        LocaleId = localeId;
    }

    public PingPacket(NetDataReader reader)
    {
        PingLocation = reader.GetUnmanaged<Vector3>();
        PingType = reader.GetEnum<PingFactory.EPingType>();
        PingColor = reader.GetUnmanaged<Color>();
        Nickname = reader.GetString();
        LocaleId = reader.GetString();
    }

    public readonly Vector3 PingLocation;
    public readonly PingFactory.EPingType PingType;
    public readonly Color PingColor;
    public readonly string Nickname;
    public readonly string LocaleId;

    public readonly void Execute()
    {
        if (FikaPlugin.Instance.Settings.UsePingSystem.Value && !FikaBackendUtils.IsHeadless)
        {
            PingFactory.ReceivePing(PingLocation, PingType, PingColor, Nickname, LocaleId);
        }
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.PutUnmanaged(PingLocation);
        writer.PutEnum(PingType);
        writer.PutUnmanaged(PingColor);
        writer.Put(Nickname);
        writer.Put(LocaleId);
    }
}
