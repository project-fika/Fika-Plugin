using System;
using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public sealed class ReloadMagPacket : IPoolSubPacket
{
    private ReloadMagPacket()
    {

    }

    public static ReloadMagPacket FromValue(MongoID magId, ItemAddress gridItemAddress)
    {
        var packet = FirearmSubPacketPoolManager.Instance.GetPacket<ReloadMagPacket>(EFirearmSubPacketType.ReloadMag);
        packet.MagId = magId;
        packet.GridItemAddress = gridItemAddress;
        return packet;
    }

    public static ReloadMagPacket CreateInstance()
    {
        return new();
    }

    public MongoID MagId;
    public ItemAddress GridItemAddress;
    public ItemAddressDescriptor Descriptor;

    public void Execute(FikaPlayer player)
    {
        if (player.HandsController is ObservedFirearmController controller)
        {
            Magazine magazine = null;
            try
            {
                var result = player.FindItemById(MagId);
                if (!result.Succeeded)
                {
                    FikaGlobals.LogError(result.Error.ToString());
                    return;
                }
                if (result.Value is Magazine magazineClass)
                {
                    magazine = magazineClass;
                }
                else
                {
                    FikaGlobals.LogError($"ReloadMagPacket: Item was not MagazineClass, it was {result.Value.GetType()}");
                }
            }
            catch (Exception ex)
            {
                FikaGlobals.LogError(ex);
                FikaGlobals.LogError($"ReloadMagPacket: There is no item {MagId} in profile {player.ProfileId}");
                throw;
            }
            ItemAddress gridItemAddress = null;
            if (Descriptor != null)
            {
                try
                {
                    gridItemAddress = player.InventoryController.ToItemAddress(Descriptor);
                }
                catch (HTTPNetworkException exception2)
                {
                    FikaGlobals.LogError(exception2);
                }
            }
            if (magazine != null)
            {
                controller.FastForwardCurrentState();
                controller.ReloadMag(magazine, gridItemAddress, null);
            }
            else
            {
                FikaGlobals.LogError($"ReloadMagPacket: final variables were null! Mag: {magazine}, Address: {gridItemAddress}");
            }
        }
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.PutMongoID(MagId);
        var exists = GridItemAddress != null;
        writer.Put(exists);
        if (exists)
        {
            writer.PutPolymorph(GridItemAddress.ToDescriptor());
        }
    }

    public void Deserialize(NetDataReader reader)
    {
        MagId = reader.GetMongoID();
        var exists = reader.GetBool();
        if (exists)
        {
            Descriptor = reader.GetPolymorph<ItemAddressDescriptor>();
        }
    }

    public void Dispose()
    {
        MagId = default;
        GridItemAddress = null;
        Descriptor = null;
    }
}
