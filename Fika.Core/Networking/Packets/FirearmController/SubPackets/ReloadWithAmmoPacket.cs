using System.Buffers;
using EFT.InventoryLogic;
using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public readonly struct ReloadWithAmmoPacket : IFirearmPacket
{
    public readonly EReloadWithAmmoStatus Status;
    public readonly ushort AmmoCount;
    public readonly string[] AmmoIds;

    public EFirearmPacketType Type => EFirearmPacketType.ReloadWithAmmo;

    public ReloadWithAmmoPacket(EReloadWithAmmoStatus status, string[] ammoIds = null)
    {
        Status = status;
        AmmoCount = (ushort)(ammoIds?.Length ?? 0);
        AmmoIds = ammoIds ?? [];
    }

    public ReloadWithAmmoPacket(NetDataReader reader)
    {
        Status = reader.GetEnum<EReloadWithAmmoStatus>();

        if (Status == EReloadWithAmmoStatus.StartReload)
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

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.PutEnum(Status);

        if (Status == EReloadWithAmmoStatus.StartReload)
        {
            writer.Put(AmmoCount);
            for (var i = 0; i < AmmoCount; i++)
            {
                writer.Put(AmmoIds[i]);
            }
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
                    controller.CurrentOperation.ReloadWithAmmo(ammoPack, null, null);
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