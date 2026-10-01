using EFT;
using EFT.InventoryLogic.Operations;

namespace Fika.Core.Networking.Packets.Generic.SubPackets;

public readonly struct InventoryPacket : IGenericPacket
{
    public EGenericPacketType Type => EGenericPacketType.InventoryOperation;

    public InventoryPacket(int netId, AbstractOperation operation)
    {
        NetId = netId;
        CallbackId = operation.Id;
        Descriptor = operation.ToDescriptor();
    }

    public InventoryPacket(NetDataReader reader)
    {
        NetId = reader.GetInt();
        CallbackId = reader.GetUShort();
        Descriptor = reader.GetPolymorph<InventoryOperationDescriptor>();
    }

    public readonly int NetId;
    public readonly ushort CallbackId;
    public readonly InventoryOperationDescriptor Descriptor;

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.Put(CallbackId);
        writer.PutPolymorph(Descriptor);
    }
}
