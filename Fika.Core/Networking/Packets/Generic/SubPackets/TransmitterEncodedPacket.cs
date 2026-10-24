using Fika.Core.Main.Components;

namespace Fika.Core.Networking.Packets.Generic.SubPackets;

public readonly struct TransmitterEncodedPacket : IGenericPacket
{
    public EGenericPacketType Type => EGenericPacketType.TransmitterEncoded;

    public TransmitterEncodedPacket(int netId, bool isEncoded)
    {
        NetId = netId;
        IsEncoded = isEncoded;
    }

    public TransmitterEncodedPacket(NetDataReader reader)
    {
        NetId = reader.GetInt();
        IsEncoded = reader.GetBool();
    }

    public readonly int NetId;
    public readonly bool IsEncoded;

    public readonly void Execute()
    {
        if (!CoopHandler.TryGetCoopHandler(out var coopHandler))
        {
            return;
        }

        var myPlayer = coopHandler.MyPlayer;
        if (myPlayer == null || myPlayer.NetId != NetId)
        {
            return;
        }

        var transmitter = myPlayer.FindRadioTransmitter();
        if (transmitter != null && transmitter.Handler != null)
        {
            transmitter.Handler.SetEncoded(IsEncoded);
        }
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.Put(IsEncoded);
    }
}
