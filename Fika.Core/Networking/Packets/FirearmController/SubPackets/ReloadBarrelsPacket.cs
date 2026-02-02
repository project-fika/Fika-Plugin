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

    public static ReloadBarrelsPacket FromValue(bool reload, string[] ammoIds, byte[] locationDescription)
    {
        var packet = FirearmSubPacketPoolManager.Instance.GetPacket<ReloadBarrelsPacket>(EFirearmSubPacketType.ReloadBarrels);
        packet.Reload = reload;
        packet.AmmoIds = ammoIds;
        packet.LocationDescription = locationDescription;
        return packet;
    }

    public static ReloadBarrelsPacket CreateInstance()
    {
        return new();
    }

    public string[] AmmoIds;
    public byte[] LocationDescription;
    public bool Reload;

    public void Execute(FikaPlayer player)
    {
        if (player.HandsController is ObservedFirearmController controller)
        {
            var ammo = controller.FindAmmoByIds(AmmoIds);
            AmmoPackReloadingClass ammoPack = new(ammo);
            ItemAddress gridItemAddress = null;

            using var eftReader = PacketToEFTReaderAbstractClass.Get(LocationDescription);
            try
            {
                if (LocationDescription.Length > 0)
                {
                    var descriptor = eftReader.ReadPolymorph<GClass1950>();
                    gridItemAddress = player.InventoryController.ToItemAddress(descriptor);
                }
            }
            catch (GException4 exception2)
            {
                FikaGlobals.LogError(exception2);
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
        writer.Put(Reload);
        if (Reload)
        {
            writer.PutArray(AmmoIds);
            writer.PutByteArray(LocationDescription);
        }
    }

    public void Deserialize(NetDataReader reader)
    {
        Reload = reader.GetBool();
        if (Reload)
        {
            AmmoIds = reader.GetStringArray();
            LocationDescription = reader.GetByteArray();
        }
    }

    public void Dispose()
    {
        Reload = false;
        AmmoIds = null;
        LocationDescription = null;
    }
}
