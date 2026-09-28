using EFT.InventoryLogic;
using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public readonly struct CylinderMagPacket : IFirearmPacket
{
    public CylinderMagPacket(EReloadWithAmmoStatus status, int camoraIndex, int ammoLoadedToMag, bool changed, bool hammerClosed, string[] ammoIds)
    {
        Status = status;
        CamoraIndex = camoraIndex;
        AmmoLoadedToMag = ammoLoadedToMag;
        Changed = changed;
        HammerClosed = hammerClosed;
        AmmoIds = ammoIds;
    }

    public CylinderMagPacket(NetDataReader reader)
    {
        Changed = reader.GetBool();
        if (Changed)
        {
            CamoraIndex = reader.GetInt();
            HammerClosed = reader.GetBool();
        }
        Status = reader.GetEnum<EReloadWithAmmoStatus>();
        AmmoLoadedToMag = reader.GetInt();
        AmmoIds = reader.GetStringArray();
    }

    public readonly EReloadWithAmmoStatus Status;
    public readonly int CamoraIndex;
    public readonly int AmmoLoadedToMag;
    public readonly bool Changed;
    public readonly bool HammerClosed;
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
                controller.CurrentOperation.ReloadCylinderMagazine(ammoPack, null, null);
            }

            if (Changed && controller.Weapon.GetCurrentMagazine() is CylinderMagazine cylinder)
            {
                cylinder.SetCurrentCamoraIndex(CamoraIndex);
                controller.Weapon.CylinderHammerClosed = HammerClosed;
            }
        }
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(Changed);
        if (Changed)
        {
            writer.Put(CamoraIndex);
            writer.Put(HammerClosed);
        }
        writer.PutEnum(Status);
        writer.Put(AmmoLoadedToMag);
        writer.PutArray(AmmoIds);
    }

    public EFirearmPacketType Type => EFirearmPacketType.CylinderMag;
}
