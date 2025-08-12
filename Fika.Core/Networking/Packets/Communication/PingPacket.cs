using Fika.Core.Main.Factories;

namespace Fika.Core.Networking.Packets.Communication;

public struct PingPacket : INetSerializable
{
    public Vector3 PingLocation;
    public PingFactory.EPingType PingType;
    public Color PingColor;
    public string Nickname;
    public string LocaleId;

    public void Deserialize(NetDataReader reader)
    {
        PingLocation = reader.GetUnmanaged<Vector3>();
        PingType = reader.GetEnum<PingFactory.EPingType>();
        PingColor = reader.GetUnmanaged<Color>();
        Nickname = reader.GetString();
        LocaleId = reader.GetString();
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
