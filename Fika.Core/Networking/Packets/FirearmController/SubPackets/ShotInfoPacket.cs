using EFT;
using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public readonly struct ShotInfoPacket : IFirearmPacket
{
    public readonly Vector3 ShotPosition;
    public readonly Vector3 ShotDirection;
    public readonly MongoID AmmoTemplate;
    public readonly float Overheat;
    public readonly float LastShotOverheat;
    public readonly float LastShotTime;
    public readonly float Durability;
    public readonly int ChamberIndex;
    public readonly bool UnderbarrelShot;
    public readonly bool SlideOnOverheatReached;

    public ShotInfoPacket(Vector3 shotPosition, Vector3 shotDirection, MongoID ammoTemplate, float overheat,
        float lastShotOverheat, float lastShotTime, float durability, int chamberIndex, bool underbarrelShot,
        bool slideOnOverheatReached)
    {
        ShotPosition = shotPosition;
        ShotDirection = shotDirection;
        AmmoTemplate = ammoTemplate;
        Overheat = overheat;
        LastShotOverheat = lastShotOverheat;
        LastShotTime = lastShotTime;
        Durability = durability;
        ChamberIndex = chamberIndex;
        UnderbarrelShot = underbarrelShot;
        SlideOnOverheatReached = slideOnOverheatReached;
    }

    public ShotInfoPacket(NetDataReader reader)
    {
        ShotPosition = reader.GetUnmanaged<Vector3>();
        ShotDirection = reader.GetUnmanaged<Vector3>();
        AmmoTemplate = reader.GetMongoID();
        Overheat = reader.GetPackedFloat(0f, 200f, EFloatCompression.High);
        LastShotOverheat = reader.GetPackedFloat(0f, 200f, EFloatCompression.High);
        LastShotTime = reader.GetFloat();
        Durability = reader.GetPackedFloat(0f, 100f, EFloatCompression.High);
        ChamberIndex = reader.GetPackedInt(0, 16);
        UnderbarrelShot = reader.GetBool();
        SlideOnOverheatReached = reader.GetBool();
    }

    public readonly void Execute(FikaPlayer player)
    {
        if (!player.HealthController.IsAlive)
        {
            FikaGlobals.LogError("ShotInfoPacket::Execute: Player was not alive, can not process!");
            return;
        }

        if (player.HandsController is ObservedFirearmController controller)
        {
            controller.HandleShotInfoPacket(in this, player.InventoryController);
        }
        else
        {
            FikaGlobals.LogError($"ShotInfoPacket::Execute: HandsController was not ObservedFirearmController, was: {player.HandsController.GetType().Name}");
        }
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.PutUnmanaged(ShotPosition);
        writer.PutUnmanaged(ShotDirection);
        writer.PutMongoID(AmmoTemplate);
        writer.PutPackedFloat(Overheat, 0f, 200f, EFloatCompression.High);
        writer.PutPackedFloat(LastShotOverheat, 0f, 200f, EFloatCompression.High);
        writer.Put(LastShotTime);
        writer.PutPackedFloat(Durability, 0f, 100f, EFloatCompression.High);
        writer.PutPackedInt(ChamberIndex, 0, 16);
        writer.Put(UnderbarrelShot);
        writer.Put(SlideOnOverheatReached);
    }

    public EFirearmPacketType Type => EFirearmPacketType.ShotInfo;
}
