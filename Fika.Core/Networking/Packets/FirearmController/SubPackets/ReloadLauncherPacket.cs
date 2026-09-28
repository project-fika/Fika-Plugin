using EFT.InventoryLogic;
using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public readonly struct ReloadLauncherPacket : IFirearmPacket
{
    public ReloadLauncherPacket(bool reload, string[] ammoIds)
    {
        Reload = reload;
        AmmoIds = ammoIds;
    }

    public ReloadLauncherPacket(NetDataReader reader)
    {
        Reload = reader.GetBool();
        if (Reload)
        {
            AmmoIds = reader.GetStringArray();
        }
    }

    public readonly string[] AmmoIds;
    public readonly bool Reload;

    public readonly void Execute(FikaPlayer player)
    {
        if (player.HandsController is ObservedFirearmController controller)
        {
            var ammo = controller.FindAmmoByIds(AmmoIds);
            AmmoPack ammoPack = new(ammo);
            controller.FastForwardCurrentState();
            controller.ReloadGrenadeLauncher(ammoPack, null);
        }
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(Reload);
        if (Reload)
        {
            writer.PutArray(AmmoIds);
        }
    }

    public EFirearmPacketType Type => EFirearmPacketType.ReloadLauncher;
}
