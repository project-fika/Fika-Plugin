using System;
using System.Buffers;
using EFT.InventoryLogic;
using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public readonly struct ReloadLauncherPacket : IFirearmPacket
{
    public readonly ushort AmmoCount;
    public readonly string[] AmmoIds;
    public readonly bool Reload;

    public EFirearmPacketType Type => EFirearmPacketType.ReloadLauncher;

    public ReloadLauncherPacket(bool reload, string[] ammoIds = null)
    {
        Reload = reload;
        AmmoCount = (ushort)(ammoIds?.Length ?? 0);
        AmmoIds = ammoIds ?? [];
    }

    public ReloadLauncherPacket(NetDataReader reader)
    {
        Reload = reader.GetBool();
        if (Reload)
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
        }
        else
        {
            AmmoCount = 0;
            AmmoIds = [];
        }
    }

    public readonly void Execute(FikaPlayer player)
    {
        try
        {
            if (player.HandsController is ObservedFirearmController controller)
            {
                var ammo = controller.FindAmmoByIds(AmmoIds, AmmoCount);
                AmmoPack ammoPack = new(ammo);
                controller.FastForwardCurrentState();
                controller.ReloadGrenadeLauncher(ammoPack, null);
            }
        }
        finally
        {
            if (AmmoCount > 0)
            {
                ArrayPool<string>.Shared.Return(AmmoIds, clearArray: true);
            }
        }
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(Reload);
        if (Reload)
        {
            writer.Put(AmmoCount);
            for (var i = 0; i < AmmoCount; i++)
            {
                writer.Put(AmmoIds[i]);
            }
        }
    }
}