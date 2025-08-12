using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;
using System;

namespace Fika.Core.Networking.Packets.Generic.SubPackets;

public class InventoryPacket : IPoolSubPacket
{
    private InventoryPacket() { }

    public static InventoryPacket CreateInstance()
    {
        return new();
    }

    public static InventoryPacket FromValue(int netId, BaseInventoryOperationClass operation)
    {
        InventoryPacket packet = GenericSubPacketPoolManager.Instance.GetPacket<InventoryPacket>(EGenericSubPacketType.InventoryOperation);
        packet.NetId = netId;
        packet.CallbackId = operation.Id;
        packet.WriteOperation(operation);
        return packet;
    }

    public int NetId;
    public ushort CallbackId;
    public byte[] OperationBytes;

    private readonly EFTWriterClass _writer = new();

    [Obsolete("Not used for inventory packets", true)]
    public void Execute(FikaPlayer player = null)
    {
        // unused
    }

    public void WriteOperation(BaseInventoryOperationClass operation)
    {
        _writer.Reset();
        _writer.WritePolymorph(operation.ToDescriptor());
        OperationBytes = _writer.ToArray();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.Put(CallbackId);
        writer.PutByteArray(OperationBytes);
    }

    public void Deserialize(NetDataReader reader)
    {
        NetId = reader.GetInt();
        CallbackId = reader.GetUShort();
        OperationBytes = reader.GetByteArray();
    }

    public void Dispose()
    {
        NetId = 0;
        CallbackId = 0;
        OperationBytes = null;
    }
}
