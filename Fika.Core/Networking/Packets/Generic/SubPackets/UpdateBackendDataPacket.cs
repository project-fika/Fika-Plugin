using Comfort.Common;

namespace Fika.Core.Networking.Packets.Generic.SubPackets;

public readonly struct UpdateBackendDataPacket(int playerAmount) : IGenericPacket
{
    public EGenericPacketType Type => EGenericPacketType.UpdateBackendData;

    public readonly int PlayerAmount = playerAmount;

    public readonly void Execute()
    {
        Singleton<IFikaNetworkManager>.Instance.PlayerAmount = PlayerAmount;
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(PlayerAmount);
    }
}
