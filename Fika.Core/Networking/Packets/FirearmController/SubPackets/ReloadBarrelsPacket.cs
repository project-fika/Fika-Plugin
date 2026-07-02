using EFT.InventoryLogic;
using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public sealed class ReloadBarrelsPacket : IPoolSubPacket
{
    private ReloadBarrelsPacket()
    {

    }

    public static ReloadBarrelsPacket FromValue(string[] ammoIds, ItemAddress placeToPutContainedAmmoMagazine)
    {
        var packet = FirearmSubPacketPoolManager.Instance.GetPacket<ReloadBarrelsPacket>(EFirearmSubPacketType.ReloadBarrels);
        packet.AmmoIds = ammoIds;
        packet.PlaceToPutContainedAmmoMagazine = placeToPutContainedAmmoMagazine;
        return packet;
    }

    public static ReloadBarrelsPacket CreateInstance()
    {
        return new();
    }

    public string[] AmmoIds;
    public ItemAddress PlaceToPutContainedAmmoMagazine;
    public GClass1950 Descriptor;

    public void Execute(FikaPlayer player)
    {
        if (player.HandsController is ObservedFirearmController controller)
        {
            var ammo = controller.FindAmmoByIds(AmmoIds);
            AmmoPackReloadingClass ammoPack = new(ammo);
            ItemAddress gridItemAddress = null;

            if (Descriptor != null)
            {
                try
                {
                    gridItemAddress = player.InventoryController.ToItemAddress(Descriptor);
                }
                catch (GException4 exception2)
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

    public void Serialize(NetDataWriter writer)
    {
        writer.PutArray(AmmoIds);
        var exists = PlaceToPutContainedAmmoMagazine != null;
        writer.Put(exists);
        if (exists)
        {
            writer.PutPolymorph(PlaceToPutContainedAmmoMagazine.ToDescriptor());
        }
    }

    public void Deserialize(NetDataReader reader)
    {
        AmmoIds = reader.GetStringArray();
        var exists = reader.GetBool();
        if (exists)
        {
            Descriptor = reader.GetPolymorph<GClass1950>();
        }
    }

    public void Dispose()
    {
        AmmoIds = null;
        PlaceToPutContainedAmmoMagazine = null;
        Descriptor = null;
    }
}
