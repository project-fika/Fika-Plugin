using Fika.Core.Main.Utils;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.Backend;

public struct StashesPacket : INetSerializable
{
    public bool HasBTR;
    public byte[][] BTRData;
    public StashItemClass[] BTRStashes;

    public bool HasTransit;
    public byte[][] TransitData;
    public StashItemClass[] TransitStashes;

    public void Deserialize(NetDataReader reader)
    {
        HasBTR = reader.GetBool();
        if (HasBTR)
        {
            int amount = reader.GetInt();
            BTRData = new byte[amount][];
            BTRStashes = new StashItemClass[amount];
            for (int i = 0; i < amount; i++)
            {
                byte[] data = reader.GetByteArray();
                using GClass1283 eftReader = PacketToEFTReaderAbstractClass.Get(data);
                InventoryDescriptorClass descriptor = eftReader.ReadEFTItemDescriptor();
                BTRStashes[i] = descriptor.Deserialize<StashItemClass>();
            }

        }

        HasTransit = reader.GetBool();
        if (HasTransit)
        {
            int amount = reader.GetInt();
            TransitData = new byte[amount][];
            TransitStashes = new StashItemClass[amount];
            for (int i = 0; i < amount; i++)
            {
                byte[] data = reader.GetByteArray();
                using GClass1283 eftReader = PacketToEFTReaderAbstractClass.Get(data);
                InventoryDescriptorClass descriptor = eftReader.ReadEFTItemDescriptor();
                TransitStashes[i] = descriptor.Deserialize<StashItemClass>();
            }
        }
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(HasBTR);
        if (HasBTR)
        {
            BTRData = new byte[BTRStashes.Length][];
            writer.Put(BTRData.Length);
            for (int i = 0; i < BTRData.Length; i++)
            {
                EFTWriterClass eftWriter = WriterPoolManager.GetWriter();
                eftWriter.WriteEFTItemDescriptor(EFTItemSerializerClass.SerializeItem(BTRStashes[i],
                    FikaGlobals.SearchControllerSerializer));
                BTRData[i] = eftWriter.ToArray();
                writer.PutByteArray(BTRData[i]);
                WriterPoolManager.ReturnWriter(eftWriter);
            }
        }

        writer.Put(HasTransit);
        if (HasTransit)
        {
            TransitData = new byte[TransitStashes.Length][];
            writer.Put(TransitData.Length);
            for (int i = 0; i < TransitData.Length; i++)
            {
                EFTWriterClass eftWriter = WriterPoolManager.GetWriter();
                eftWriter.WriteEFTItemDescriptor(EFTItemSerializerClass.SerializeItem(TransitStashes[i],
                    FikaGlobals.SearchControllerSerializer));
                TransitData[i] = eftWriter.ToArray();
                writer.PutByteArray(TransitData[i]);
                WriterPoolManager.ReturnWriter(eftWriter);
            }
        }
    }
}
