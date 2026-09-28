namespace Fika.Core.Networking.Packets.FirearmController;

public interface IFirearmPacket
{
    EFirearmPacketType Type { get; }
    void Serialize(NetDataWriter writer);
}
