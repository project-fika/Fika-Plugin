using Fika.Core.Main.Utils;

namespace Fika.Core.Networking.Packets.Backend;

public struct StashesPacket : INetSerializable
{
    public bool HasBTR;
    public StashItemClass[] BTRStashes;

    public bool HasTransit;
    public StashItemClass[] TransitStashes;

    public void Deserialize(NetDataReader reader)
    {
        HasBTR = reader.GetBool();
        if (HasBTR)
        {
            var amount = reader.GetInt();
            BTRStashes = new StashItemClass[amount];
            for (var i = 0; i < amount; i++)
            {
                var descriptor = reader.GetEFTItemDescriptor();
                BTRStashes[i] = descriptor.Deserialize<StashItemClass>();
            }

        }

        HasTransit = reader.GetBool();
        if (HasTransit)
        {
            var amount = reader.GetInt();
            TransitStashes = new StashItemClass[amount];
            for (var i = 0; i < amount; i++)
            {
                var descriptor = reader.GetEFTItemDescriptor();
                TransitStashes[i] = descriptor.Deserialize<StashItemClass>();
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
                writer.PutEFTItemDescriptor(EFTItemSerializerClass.SerializeItem(BTRStashes[i],
                    FikaGlobals.SearchControllerSerializer));
            }
        }

        writer.Put(HasTransit);
        if (HasTransit)
        {
            writer.Put(TransitStashes.Length);
            for (var i = 0; i < TransitStashes.Length; i++)
            {
                writer.PutEFTItemDescriptor(EFTItemSerializerClass.SerializeItem(TransitStashes[i],
                    FikaGlobals.SearchControllerSerializer));
            }
        }
    }
}
