using Fika.Core.Main.ObservedClasses;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;
using LiteNetLib.Utils;
using System.Collections.Generic;

namespace Fika.Core.Networking.Packets.FirearmController
{
    public class CylinderMagPacket : IPoolSubPacket
    {
        private CylinderMagPacket()
        {

        }

        public static CylinderMagPacket FromValue(EReloadWithAmmoStatus status, int camoraIndex, int ammoLoadedToMag, bool changed, bool hammerClosed, bool reload, string[] ammoIds)
        {
            CylinderMagPacket packet = FirearmSubPacketPoolManager.Instance.GetPacket<CylinderMagPacket>(EFirearmSubPacketType.CylinderMag);
            packet.Status = status;
            packet.CamoraIndex = camoraIndex;
            packet.AmmoLoadedToMag = ammoLoadedToMag;
            packet.Changed = changed;
            packet.HammerClosed = hammerClosed;
            packet.Reload = reload;
            packet.AmmoIds = ammoIds;
            return packet;
        }

        public static CylinderMagPacket CreateInstance()
        {
            return new();
        }

        public EReloadWithAmmoStatus Status;
        public int CamoraIndex;
        public int AmmoLoadedToMag;
        public bool Changed;
        public bool HammerClosed;
        public bool Reload;
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
                        controller.CurrentOperation.ReloadCylinderMagazine(ammoPack, null, null);
                    }
                }

                if (Changed && controller.Weapon.GetCurrentMagazine() is CylinderMagazineItemClass cylinder)
                {
                    cylinder.SetCurrentCamoraIndex(CamoraIndex);
                    controller.Weapon.CylinderHammerClosed = HammerClosed;
                }
            }
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Changed);
            if (Changed)
            {
                writer.Put(CamoraIndex);
                writer.Put(HammerClosed);
            }
            writer.Put(Reload);
            if (Reload)
            {
                writer.PutEnum(Status);
                writer.Put(AmmoLoadedToMag);
                writer.PutArray(AmmoIds);
            }
        }

        public void Deserialize(NetDataReader reader)
        {
            Changed = reader.GetBool();
            if (Changed)
            {
                CamoraIndex = reader.GetInt();
                HammerClosed = reader.GetBool();
            }
            Reload = reader.GetBool();
            if (Reload)
            {
                Status = reader.GetEnum<EReloadWithAmmoStatus>();
                AmmoLoadedToMag = reader.GetInt();
                AmmoIds = reader.GetStringArray();
            }
        }

        public void Dispose()
        {
            Status = default;
            CamoraIndex = 0;
            AmmoLoadedToMag = 0;
            Changed = false;
            HammerClosed = false;
            Reload = false;
            AmmoIds = null;
        }
    }
}
