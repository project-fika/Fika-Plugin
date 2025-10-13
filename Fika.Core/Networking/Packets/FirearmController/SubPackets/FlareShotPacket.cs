using Comfort.Common;
using EFT;
using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public sealed class FlareShotPacket : IPoolSubPacket
{
    private FlareShotPacket()
    {

    }

    public static FlareShotPacket FromValue(Vector3 shotPosition, Vector3 shotForward, MongoID ammoTemplateId, bool startOneShotFire)
    {
        FlareShotPacket packet = FirearmSubPacketPoolManager.Instance.GetPacket<FlareShotPacket>(EFirearmSubPacketType.FlareShot);
        packet.ShotPosition = shotPosition;
        packet.ShotForward = shotForward;
        packet.AmmoTemplateId = ammoTemplateId;
        packet.StartOneShotFire = startOneShotFire;
        return packet;
    }

    public static FlareShotPacket CreateInstance()
    {
        return new();
    }

    public Vector3 ShotPosition;
    public Vector3 ShotForward;
    public MongoID AmmoTemplateId;
    public bool StartOneShotFire;

    public void Execute(FikaPlayer player)
    {
        if (player.HandsController is ObservedFirearmController controller)
        {
            if (StartOneShotFire)
            {
                controller.FirearmsAnimator.SetFire(true);

                if (controller.Weapon is not RevolverItemClass)
                {
                    controller.FirearmsAnimator.Animator.Play(controller.FirearmsAnimator.FullFireStateName, 1, 0f);
                    controller.Weapon.Repairable.Durability = 0;
                }
                else
                {
                    controller.FirearmsAnimator.Animator.Play(controller.FirearmsAnimator.FullDoubleActionFireStateName, 1, 0f);
                }
            }
            else
            {
                AmmoItemClass bulletClass = (AmmoItemClass)Singleton<ItemFactoryClass>.Instance.CreateItem(MongoID.Generate(), AmmoTemplateId, null);
                controller.InitiateFlare(bulletClass, ShotPosition, ShotForward);
                bulletClass.IsUsed = true;
                controller.WeaponManager.MoveAmmoFromChamberToShellPort(bulletClass.IsUsed, 0);
                controller.FirearmsAnimator.SetFire(false);
            }
        }
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(StartOneShotFire);
        if (!StartOneShotFire)
        {
            writer.PutUnmanaged(ShotPosition);
            writer.PutUnmanaged(ShotForward);
            writer.PutMongoID(AmmoTemplateId);
        }
    }

    public void Deserialize(NetDataReader reader)
    {
        StartOneShotFire = reader.GetBool();
        if (!StartOneShotFire)
        {
            ShotPosition = reader.GetUnmanaged<Vector3>();
            ShotForward = reader.GetUnmanaged<Vector3>();
            AmmoTemplateId = reader.GetMongoID();
        }
    }

    public void Dispose()
    {
        ShotPosition = default;
        ShotForward = default;
        AmmoTemplateId = default;
        StartOneShotFire = false;
    }
}
