using EFT.InventoryLogic;
using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public readonly struct ReloadWithAmmoPacket : IFirearmPacket
{
    public ReloadWithAmmoPacket(EReloadWithAmmoStatus status, int ammoLoadedToMag = 0, string[] ammoIds = null)
    {
        Status = status;
        AmmoLoadedToMag = ammoLoadedToMag;
        AmmoIds = ammoIds;
    }

    public ReloadWithAmmoPacket(NetDataReader reader)
    {
        Status = reader.GetEnum<EReloadWithAmmoStatus>();
        if (Status == EReloadWithAmmoStatus.StartReload)
        {
            AmmoIds = reader.GetStringArray();
        }
        AmmoLoadedToMag = reader.GetInt();
    }

    public readonly EReloadWithAmmoStatus Status;
    public readonly int AmmoLoadedToMag;
    public readonly string[] AmmoIds;

    public readonly void Execute(FikaPlayer player)
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

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.PutEnum(Status);
        if (Status == EReloadWithAmmoStatus.StartReload)
        {
            writer.PutArray(AmmoIds);
        }
        writer.Put(AmmoLoadedToMag);
    }

    public EFirearmPacketType Type => EFirearmPacketType.ReloadWithAmmo;
}
