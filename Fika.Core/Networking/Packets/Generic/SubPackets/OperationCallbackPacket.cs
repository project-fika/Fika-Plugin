using EFT;

namespace Fika.Core.Networking.Packets.Generic.SubPackets;

public readonly struct OperationCallbackPacket : IGenericPacket
{
    public EGenericPacketType Type => EGenericPacketType.OperationCallback;

    public OperationCallbackPacket(int netId, ushort callbackId, EOperationStatus status, string error = null)
    {
        NetId = netId;
        CallbackId = callbackId;
        Status = status;
        Error = error;
    }

    public OperationCallbackPacket(NetDataReader reader)
    {
        NetId = reader.GetInt();
        CallbackId = reader.GetUShort();
        Status = reader.GetEnum<EOperationStatus>();
        if (Status == EOperationStatus.Failed)
        {
            Error = reader.GetString();
        }
    }

    public readonly int NetId;
    public readonly ushort CallbackId;
    public readonly EOperationStatus Status;
    public readonly string Error;

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.Put(CallbackId);
        writer.PutEnum(Status);
        if (Status == EOperationStatus.Failed)
        {
            writer.Put(Error);
        }
    }
}
