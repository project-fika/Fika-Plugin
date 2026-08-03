using EFT.InventoryLogic;
using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public sealed class ReloadWithAmmoPacket : IPoolSubPacket
{
    private ReloadWithAmmoPacket()
    {

    }

    public static ReloadWithAmmoPacket FromValue(EReloadWithAmmoStatus status, int ammoLoadedToMag = 0, string[] ammoIds = null)
    {
        var packet = FirearmSubPacketPoolManager.Instance.GetPacket<ReloadWithAmmoPacket>(EFirearmSubPacketType.ReloadWithAmmo);
        packet.Status = status;
        packet.AmmoLoadedToMag = ammoLoadedToMag;
        packet.AmmoIds = ammoIds;
        return packet;
    }

    public static ReloadWithAmmoPacket CreateInstance()
    {
        return new();
    }

    public EReloadWithAmmoStatus Status;
    public int AmmoLoadedToMag;
    public string[] AmmoIds;

    public void Execute(FikaPlayer player)
    {
        if (player.HandsController is ObservedFirearmController controller)
        {
            if (Status == EReloadWithAmmoStatus.AbortReload)
            {
                controller.CurrentOperation.SetTriggerPressed(true);
            }

            if (Status == EReloadWithAmmoStatus.StartReload)
            {
                var bullets = controller.FindAmmoByIds(AmmoIds);
                AmmoPack ammoPack = new(bullets);
                controller.FastForwardCurrentState();
                controller.CurrentOperation.ReloadWithAmmo(ammoPack, null, null);
            }
        }
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.PutEnum(Status);
        if (Status == EReloadWithAmmoStatus.StartReload)
        {
            writer.PutArray(AmmoIds);
        }
        writer.Put(AmmoLoadedToMag);
    }

    public void Deserialize(NetDataReader reader)
    {
        Status = reader.GetEnum<EReloadWithAmmoStatus>();
        if (Status == EReloadWithAmmoStatus.StartReload)
        {
            AmmoIds = reader.GetStringArray();
        }
        AmmoLoadedToMag = reader.GetInt();
    }

    public void Dispose()
    {
        Status = EReloadWithAmmoStatus.None;
        AmmoLoadedToMag = 0;
        AmmoIds = null;
    }
}
