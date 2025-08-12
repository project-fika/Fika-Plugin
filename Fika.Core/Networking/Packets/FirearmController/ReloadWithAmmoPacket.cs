using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;
using Fika.Core.Networking.LiteNetLib.Utils;
using Fika.Core.Networking.Pooling;
using System.Collections.Generic;

namespace Fika.Core.Networking.Packets.FirearmController;

public class ReloadWithAmmoPacket : IPoolSubPacket
{
    private ReloadWithAmmoPacket()
    {

    }

    public static ReloadWithAmmoPacket FromValue(bool reload, EReloadWithAmmoStatus status, int ammoLoadedToMag = 0, string[] ammoIds = null)
    {
        ReloadWithAmmoPacket packet = FirearmSubPacketPoolManager.Instance.GetPacket<ReloadWithAmmoPacket>(EFirearmSubPacketType.ReloadWithAmmo);
        packet.Reload = reload;
        packet.Status = status;
        packet.AmmoLoadedToMag = ammoLoadedToMag;
        packet.AmmoIds = ammoIds;
        return packet;
    }

    public static ReloadWithAmmoPacket CreateInstance()
    {
        return new();
    }

    public bool Reload;
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

            if (Reload)
            {
                if (Status == EReloadWithAmmoStatus.StartReload)
                {
                    List<AmmoItemClass> bullets = controller.FindAmmoByIds(AmmoIds);
                    AmmoPackReloadingClass ammoPack = new(bullets);
                    controller.FastForwardCurrentState();
                    controller.CurrentOperation.ReloadWithAmmo(ammoPack, null, null);
                }
            }
        }
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Reload);
        if (Reload)
        {
            writer.PutEnum(Status);
            if (Status == EReloadWithAmmoStatus.StartReload)
            {
                writer.PutArray(AmmoIds);
            }
            if (AmmoLoadedToMag > 0)
            {
                writer.Put(AmmoLoadedToMag);
            }
        }
    }

    public void Deserialize(NetDataReader reader)
    {
        Reload = reader.GetBool();
        if (Reload)
        {
            Status = reader.GetEnum<EReloadWithAmmoStatus>();
            if (Status == EReloadWithAmmoStatus.StartReload)
            {
                AmmoIds = reader.GetStringArray();
            }
            if (Status is EReloadWithAmmoStatus.EndReload or EReloadWithAmmoStatus.AbortReload)
            {
                AmmoLoadedToMag = reader.GetInt();
            }
        }
    }

    public void Dispose()
    {
        Reload = false;
        Status = EReloadWithAmmoStatus.None;
        AmmoLoadedToMag = 0;
        AmmoIds = null;
    }
}
