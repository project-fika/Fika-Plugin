using System;
using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public readonly struct ReloadMagPacket : IFirearmPacket
{
    public ReloadMagPacket(MongoID magId, ItemAddress gridItemAddress)
    {
        MagId = magId;
        if (gridItemAddress != null)
        {
            Descriptor = gridItemAddress.ToDescriptor();
        }
    }

    public ReloadMagPacket(NetDataReader reader)
    {
        MagId = reader.GetMongoID();
        var exists = reader.GetBool();
        if (exists)
        {
            Descriptor = reader.GetPolymorph<ItemAddressDescriptor>();
        }
    }

    public readonly MongoID MagId;
    public readonly ItemAddressDescriptor Descriptor;

    public readonly void Execute(FikaPlayer player)
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

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.PutMongoID(MagId);
        var exists = Descriptor != null;
        writer.Put(exists);
        if (exists)
        {
            writer.PutPolymorph(Descriptor);
        }
    }

    public EFirearmPacketType Type => EFirearmPacketType.ReloadMag;
}
