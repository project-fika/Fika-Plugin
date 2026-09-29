using System;
using System.Buffers;
using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public readonly struct ReloadBarrelsPacket : IFirearmPacket
{
    public readonly ushort AmmoCount;
    public readonly string[] AmmoIds;
    public readonly ItemAddressDescriptor Descriptor;

    public EFirearmPacketType Type => EFirearmPacketType.ReloadBarrels;

    public ReloadBarrelsPacket(string[] ammoIds, ItemAddress placeToPutContainedAmmoMagazine)
    {
        AmmoCount = (ushort)(ammoIds?.Length ?? 0);
        AmmoIds = ammoIds ?? [];
        if (placeToPutContainedAmmoMagazine != null)
        {
            Descriptor = placeToPutContainedAmmoMagazine.ToDescriptor();
        }
    }

    public ReloadBarrelsPacket(NetDataReader reader)
    {
        AmmoCount = reader.GetUShort();
        if (AmmoCount > 0)
        {
            AmmoIds = ArrayPool<string>.Shared.Rent(AmmoCount);
            for (var i = 0; i < AmmoCount; i++)
            {
                AmmoIds[i] = reader.GetString();
            }
        }
        else
        {
            AmmoIds = [];
        }

        var exists = reader.GetBool();
        if (exists)
        {
            Descriptor = reader.GetPolymorph<ItemAddressDescriptor>();
        }
    }

    public readonly void Execute(FikaPlayer player)
    {
        try
        {
            if (player.HandsController is not ObservedFirearmController controller)
            {
                return;
            }

            var ammo = controller.FindAmmoByIds(AmmoIds, AmmoCount);
            if (ammo == null)
            {
                FikaGlobals.LogError($"ReloadBarrelsPacket: Failed retrieving ammo items for player {player.ProfileId}.");
                return;
            }

            AmmoPack ammoPack = new(ammo);

            ItemAddress gridItemAddress = null;
            if (Descriptor != null)
            {
                try
                {
                    gridItemAddress = player.InventoryController.ToItemAddress(Descriptor);
                }
                catch (Exception exception)
                {
                    FikaGlobals.LogError(exception);
                }
            }

            controller.FastForwardCurrentState();
            controller.ReloadBarrels(ammoPack, gridItemAddress, null);
        }
        finally
        {
            if (AmmoCount > 0 && AmmoIds != null)
            {
                ArrayPool<string>.Shared.Return(AmmoIds, true);
            }
        }
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(AmmoCount);
        for (var i = 0; i < AmmoCount; i++)
        {
            writer.Put(AmmoIds[i]);
        }

        var exists = Descriptor != null;
        writer.Put(exists);
        if (exists)
        {
            writer.PutPolymorph(Descriptor);
        }
    }
}