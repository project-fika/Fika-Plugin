using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public readonly struct FlareShotPacket : IFirearmPacket
{
    public FlareShotPacket(Vector3 shotPosition, Vector3 shotForward, MongoID ammoTemplateId, bool startOneShotFire)
    {
        ShotPosition = shotPosition;
        ShotForward = shotForward;
        AmmoTemplateId = ammoTemplateId;
        StartOneShotFire = startOneShotFire;
    }

    public FlareShotPacket(NetDataReader reader)
    {
        StartOneShotFire = reader.GetBool();
        if (!StartOneShotFire)
        {
            ShotPosition = reader.GetUnmanaged<Vector3>();
            ShotForward = reader.GetUnmanaged<Vector3>();
            AmmoTemplateId = reader.GetMongoID();
        }
    }

    public readonly Vector3 ShotPosition;
    public readonly Vector3 ShotForward;
    public readonly MongoID AmmoTemplateId;
    public readonly bool StartOneShotFire;

    public readonly void Execute(FikaPlayer player)
    {
        if (player.HandsController is ObservedFirearmController controller)
        {
            if (StartOneShotFire)
            {
                controller.FirearmsAnimator.SetFire(true);

                if (controller.Weapon is not Revolver)
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
                var bulletClass = (Ammo)Singleton<ItemFactory>.Instance.CreateItem(MongoID.Generate(), AmmoTemplateId, null);
                controller.InitiateFlare(bulletClass, ShotPosition, ShotForward);
                bulletClass.IsUsed = true;
                controller.WeaponManager.MoveAmmoFromChamberToShellPort(bulletClass.IsUsed, 0);
                controller.FirearmsAnimator.SetFire(false);
            }
        }
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(StartOneShotFire);
        if (!StartOneShotFire)
        {
            writer.PutUnmanaged(ShotPosition);
            writer.PutUnmanaged(ShotForward);
            writer.PutMongoID(AmmoTemplateId);
        }
    }

    public EFirearmPacketType Type => EFirearmPacketType.FlareShot;
}
