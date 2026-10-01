namespace Fika.Core.Networking.Packets.Generic;

public interface IGenericPacket
{
    EGenericPacketType Type { get; }
    public void Serialize(NetDataWriter writer);
}
