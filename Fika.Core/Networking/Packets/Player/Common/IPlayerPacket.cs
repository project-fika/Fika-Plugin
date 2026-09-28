namespace Fika.Core.Networking.Packets.Player.Common;

public interface IPlayerPacket
{
    EPlayerPacketType Type { get; }
    public void Serialize(NetDataWriter writer);
}
