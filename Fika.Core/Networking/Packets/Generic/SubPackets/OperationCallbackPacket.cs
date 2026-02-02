using System;
using EFT;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.Generic.SubPackets;

public sealed class OperationCallbackPacket : IPoolSubPacket
{
    private OperationCallbackPacket() { }

    public static OperationCallbackPacket CreateInstance()
    {
        return new();
    }

    public static OperationCallbackPacket FromValue(int netId, ushort callbackId, EOperationStatus status, string error = null)
    {
        var packet = GenericSubPacketPoolManager.Instance.GetPacket<OperationCallbackPacket>(EGenericSubPacketType.OperationCallback);
        packet.NetId = netId;
        packet.CallbackId = callbackId;
        packet.Status = status;
        packet.Error = error;
        return packet;
    }

    public int NetId;
    public ushort CallbackId;
    public EOperationStatus Status;
    public string Error;

    [Obsolete("Not used for inventory packets", true)]
    public void Execute(FikaPlayer player = null)
    {
        // unused
    }

    public void Deserialize(NetDataReader reader)
    {
        NetId = reader.GetInt();
        CallbackId = reader.GetUShort();
        Status = reader.GetEnum<EOperationStatus>();
        if (Status == EOperationStatus.Failed)
        {
            Error = reader.GetString();
        }
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.Put(CallbackId);
        writer.PutEnum(Status);
        if (Status == EOperationStatus.Failed)
        {
            writer.Put(Error);
        }
    }

    public void Dispose()
    {
        NetId = 0;
        CallbackId = 0;
        Status = default;
        Error = null;
    }
}
