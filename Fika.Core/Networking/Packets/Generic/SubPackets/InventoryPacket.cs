using EFT;
using System;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.Generic.SubPackets;

public sealed class InventoryPacket : IPoolSubPacket
{
    private InventoryPacket() { }

    public static InventoryPacket CreateInstance()
    {
        return new();
    }

    public static InventoryPacket FromValue(int netId, EFT.InventoryLogic.Operations.AbstractOperation operation)
    {
        var packet = GenericSubPacketPoolManager.Instance.GetPacket<InventoryPacket>(EGenericSubPacketType.InventoryOperation);
        packet.NetId = netId;
        packet.CallbackId = operation.Id;
        packet.Descriptor = operation.ToDescriptor();
        return packet;
    }

    public int NetId;
    public ushort CallbackId;
    public InventoryOperationDescriptor Descriptor;

    [Obsolete("Not used for inventory packets", true)]
    public void Execute(FikaPlayer player = null)
    {
        // unused
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.Put(CallbackId);
        writer.PutPolymorph(Descriptor);
    }

    public void Deserialize(NetDataReader reader)
    {
        NetId = reader.GetInt();
        CallbackId = reader.GetUShort();
        Descriptor = reader.GetPolymorph<InventoryOperationDescriptor>();
    }

    public void Dispose()
    {
        NetId = 0;
        CallbackId = 0;
        Descriptor = null;
    }
}
