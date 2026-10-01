using System;
using System.Buffers;
using EFT.InventoryLogic;
using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public readonly struct CylinderMagPacket : IFirearmPacket
{
    public readonly EReloadWithAmmoStatus Status;
    public readonly int CamoraIndex;
    public readonly bool Changed;
    public readonly bool HammerClosed;
    public readonly ushort AmmoCount;
    public readonly string[] AmmoIds;

    public EFirearmPacketType Type => EFirearmPacketType.CylinderMag;

    public CylinderMagPacket(EReloadWithAmmoStatus status, int camoraIndex, bool changed, bool hammerClosed, string[] ammoIds = null)
    {
        Status = status;
        CamoraIndex = camoraIndex;
        Changed = changed;
        HammerClosed = hammerClosed;
        AmmoCount = (ushort)(ammoIds?.Length ?? 0);
        AmmoIds = ammoIds ?? [];
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
        writer.Put(AmmoCount);

        for (var i = 0; i < AmmoCount; i++)
        {
            writer.Put(AmmoIds[i]);
        }
    }

    public readonly void Execute(FikaPlayer player)
    {
        try
        {
            if (player.HandsController is ObservedFirearmController controller)
            {
                if (Status == EReloadWithAmmoStatus.AbortReload)
                {
                    controller.CurrentOperation.SetTriggerPressed(true);
                }

                if (Status == EReloadWithAmmoStatus.StartReload && AmmoCount > 0)
                {
                    var bullets = controller.FindAmmoByIds(AmmoIds, AmmoCount);
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
        finally
        {
            if (AmmoCount > 0)
            {
                ArrayPool<string>.Shared.Return(AmmoIds, true);
            }
        }
    }
}