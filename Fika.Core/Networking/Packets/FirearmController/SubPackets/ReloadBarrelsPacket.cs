using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public readonly struct ReloadBarrelsPacket : IFirearmPacket
{
    public ReloadBarrelsPacket(string[] ammoIds, ItemAddress placeToPutContainedAmmoMagazine)
    {
        AmmoIds = ammoIds;
        PlaceToPutContainedAmmoMagazine = placeToPutContainedAmmoMagazine;
    }

    public ReloadBarrelsPacket(NetDataReader reader)
    {
        AmmoIds = reader.GetStringArray();
        var exists = reader.GetBool();
        if (exists)
        {
            Descriptor = reader.GetPolymorph<ItemAddressDescriptor>();
        }
    }

    public readonly string[] AmmoIds;
    public readonly ItemAddress PlaceToPutContainedAmmoMagazine;
    public readonly ItemAddressDescriptor Descriptor;

    public readonly void Execute(FikaPlayer player)
    {
        if (player.HandsController is ObservedFirearmController controller)
        {
            var ammo = controller.FindAmmoByIds(AmmoIds);
            AmmoPack ammoPack = new(ammo);
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

            if (ammoPack != null)
            {
                controller.FastForwardCurrentState();
                controller.ReloadBarrels(ammoPack, gridItemAddress, null);
            }
            else
            {
                FikaGlobals.LogError($"ReloadBarrelsPacket: final variables were null! Ammo: {ammoPack}, Address: {gridItemAddress}");
            }
        }
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.PutArray(AmmoIds);
        var exists = PlaceToPutContainedAmmoMagazine != null;
        writer.Put(exists);
        if (exists)
        {
            writer.PutPolymorph(PlaceToPutContainedAmmoMagazine.ToDescriptor());
        }
    }

    public EFirearmPacketType Type => EFirearmPacketType.ReloadBarrels;
}
