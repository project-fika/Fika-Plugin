using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.Utils;

namespace Fika.Core.Networking.Packets.Backend;

public struct StashesPacket : INetSerializable
{
    public bool HasBTR;
    public Stash[] BTRStashes;

    public bool HasTransit;
    public Stash[] TransitStashes;

    public void Deserialize(NetDataReader reader)
    {
        HasBTR = reader.GetBool();
        if (HasBTR)
        {
            var amount = reader.GetInt();
            BTRStashes = new Stash[amount];
            for (var i = 0; i < amount; i++)
            {
                var descriptor = reader.GetEFTItemDescriptor();
                BTRStashes[i] = descriptor.Deserialize<Stash>();
            }

        }

        HasTransit = reader.GetBool();
        if (HasTransit)
        {
            var amount = reader.GetInt();
            TransitStashes = new Stash[amount];
            for (var i = 0; i < amount; i++)
            {
                var descriptor = reader.GetEFTItemDescriptor();
                TransitStashes[i] = descriptor.Deserialize<Stash>();
            }
        }
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(HasBTR);
        if (HasBTR)
        {
            writer.Put(BTRStashes.Length);
            for (var i = 0; i < BTRStashes.Length; i++)
            {
                writer.PutEFTItemDescriptor(ItemBinarySerializer.SerializeItem(BTRStashes[i],
                    FikaGlobals.SearchControllerSerializer));
            }
        }

        writer.Put(HasTransit);
        if (HasTransit)
        {
            writer.Put(TransitStashes.Length);
            for (var i = 0; i < TransitStashes.Length; i++)
            {
                writer.PutEFTItemDescriptor(ItemBinarySerializer.SerializeItem(TransitStashes[i],
                    FikaGlobals.SearchControllerSerializer));
            }
        }
    }
}
