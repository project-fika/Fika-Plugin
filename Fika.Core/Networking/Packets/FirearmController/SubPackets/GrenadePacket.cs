using EFT.InventoryLogic;
using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public readonly struct GrenadePacket : IFirearmPacket
{
    public GrenadePacket(Quaternion grenadeRotation, Vector3 grenadePosition, Vector3 throwForce,
        EGrenadePacketType type, bool hasGrenade, bool lowThrow, bool plantTripwire, bool changeToIdle, bool changeToPlant)
    {
        GrenadeRotation = grenadeRotation;
        GrenadePosition = grenadePosition;
        ThrowForce = throwForce;
        GrenadeType = type;
        HasGrenade = hasGrenade;
        LowThrow = lowThrow;
        PlantTripwire = plantTripwire;
        ChangeToIdle = changeToIdle;
        ChangeToPlant = changeToPlant;
    }

    public GrenadePacket(NetDataReader reader)
    {
        GrenadeType = reader.GetEnum<EGrenadePacketType>();
        HasGrenade = reader.GetBool();
        if (HasGrenade)
        {
            GrenadeRotation = reader.GetUnmanaged<Quaternion>();
            GrenadePosition = reader.GetUnmanaged<Vector3>();
            ThrowForce = reader.GetUnmanaged<Vector3>();
            LowThrow = reader.GetBool();
        }
        PlantTripwire = reader.GetBool();
        ChangeToIdle = reader.GetBool();
        ChangeToPlant = reader.GetBool();
    }

    public readonly Quaternion GrenadeRotation;
    public readonly Vector3 GrenadePosition;
    public readonly Vector3 ThrowForce;
    public readonly EGrenadePacketType GrenadeType;
    public readonly bool HasGrenade;
    public readonly bool LowThrow;
    public readonly bool PlantTripwire;
    public readonly bool ChangeToIdle;
    public readonly bool ChangeToPlant;

    public readonly void Execute(FikaPlayer player)
    {
        if (player.HandsController is ObservedGrenadeController controller)
        {
            switch (GrenadeType)
            {
                case EGrenadePacketType.ExamineWeapon:
                    {
                        controller.ExamineWeapon();
                        break;
                    }
                case EGrenadePacketType.HighThrow:
                    {
                        controller.HighThrow();
                        break;
                    }
                case EGrenadePacketType.LowThrow:
                    {
                        controller.LowThrow();
                        break;
                    }
                case EGrenadePacketType.PullRingForHighThrow:
                    {
                        controller.PullRingForHighThrow();
                        break;
                    }
                case EGrenadePacketType.PullRingForLowThrow:
                    {
                        controller.PullRingForLowThrow();
                        break;
                    }
            }
            if (HasGrenade)
            {
                controller.SpawnGrenade(0f, GrenadePosition, GrenadeRotation, ThrowForce, LowThrow);
            }

            if (PlantTripwire)
            {
                controller.PlantTripwire();
            }

            if (ChangeToIdle)
            {
                controller.ChangeFireMode(Weapon.EFireMode.grenadeThrowing);
            }

            if (ChangeToPlant)
            {
                controller.ChangeFireMode(Weapon.EFireMode.greanadePlanting);
            }
        }
        else if (player.HandsController is ObservedQuickGrenadeController quickGrenadeController)
        {
            if (HasGrenade)
            {
                quickGrenadeController.SpawnGrenade(0f, GrenadePosition, GrenadeRotation, ThrowForce, LowThrow);
            }
        }
        else
        {
            FikaGlobals.LogError($"GrenadePacket: HandsController was not of type CoopObservedGrenadeController! Was {player.HandsController.GetType().Name}");
        }
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.PutEnum(GrenadeType);
        writer.Put(HasGrenade);
        if (HasGrenade)
        {
            writer.PutUnmanaged(GrenadeRotation);
            writer.PutUnmanaged(GrenadePosition);
            writer.PutUnmanaged(ThrowForce);
            writer.Put(LowThrow);
        }
        writer.Put(PlantTripwire);
        writer.Put(ChangeToIdle);
        writer.Put(ChangeToPlant);
    }

    public EFirearmPacketType Type => EFirearmPacketType.Grenade;
}
