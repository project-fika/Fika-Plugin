using EFT.Communications;

namespace Fika.Core.Networking.Packets.Communication;

public struct MessagePacket : INetSerializable
{
    public string Message;
    public ENotificationDurationType NotificationDurationType;
    public ENotificationIconType NotificationIconType;
    public Color Color;

    public void Deserialize(NetDataReader reader)
    {
        Message = reader.GetString();
        NotificationDurationType = reader.GetEnum<ENotificationDurationType>();
        NotificationIconType = reader.GetEnum<ENotificationIconType>();
        Color = reader.GetUnmanaged<Color>();
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(Message);
        writer.PutEnum(NotificationDurationType);
        writer.PutEnum(NotificationIconType);
        writer.PutUnmanaged(Color);
    }
}
