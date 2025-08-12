using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;
using Fika.Core.Networking.LiteNetLib.Utils;
using Fika.Core.Networking.Pooling;
using System.Collections.Generic;

namespace Fika.Core.Networking.Packets.FirearmController;

public class ReloadLauncherPacket : IPoolSubPacket
{
    private ReloadLauncherPacket()
    {

    }

    public static ReloadLauncherPacket FromValue(bool reload, string[] ammoIds)
    {
        ReloadLauncherPacket packet = FirearmSubPacketPoolManager.Instance.GetPacket<ReloadLauncherPacket>(EFirearmSubPacketType.ReloadLauncher);
        packet.Reload = reload;
        packet.AmmoIds = ammoIds;
        return packet;
    }

    public static ReloadLauncherPacket CreateInstance()
    {
        return new();
    }

    public string[] AmmoIds;
    public bool Reload;

    public void Execute(FikaPlayer player)
    {
        if (player.HandsController is ObservedFirearmController controller)
        {
            List<AmmoItemClass> ammo = controller.FindAmmoByIds(AmmoIds);
            AmmoPackReloadingClass ammoPack = new(ammo);
            controller.FastForwardCurrentState();
            controller.ReloadGrenadeLauncher(ammoPack, null);
        }
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Reload);
        if (Reload)
        {
            writer.PutArray(AmmoIds);
        }
    }

    public void Deserialize(NetDataReader reader)
    {
        Reload = reader.GetBool();
        if (Reload)
        {
            AmmoIds = reader.GetStringArray();
        }
    }

    public void Dispose()
    {
        Reload = false;
        AmmoIds = null;
    }
}
