using EFT;
using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public sealed class ShotInfoPacket : IPoolSubPacket
{
    private ShotInfoPacket()
    {

    }

    public static ShotInfoPacket CreateInstance()
    {
        return new();
    }

    public Vector3 ShotPosition;
    public Vector3 ShotDirection;
    public MongoID AmmoTemplate;
    public float Overheat;
    public float LastShotOverheat;
    public float LastShotTime;
    public float Durability;
    public int ChamberIndex;
    public bool UnderbarrelShot;
    public bool SlideOnOverheatReached;
    public EShotType ShotType;

    public static ShotInfoPacket FromDryShot(int chamberIndex, bool underbarrelShot, EShotType shotType)
    {
        var packet = FirearmSubPacketPoolManager.Instance.GetPacket<ShotInfoPacket>(EFirearmSubPacketType.ShotInfo);
        packet.ShotType = shotType;
        packet.ChamberIndex = chamberIndex;
        packet.UnderbarrelShot = underbarrelShot;
        return packet;
    }

    public static ShotInfoPacket FromMisfire(MongoID ammoTemplate, float overheat, EShotType shotType)
    {
        var packet = FirearmSubPacketPoolManager.Instance.GetPacket<ShotInfoPacket>(EFirearmSubPacketType.ShotInfo);
        packet.AmmoTemplate = ammoTemplate;
        packet.Overheat = overheat;
        packet.ShotType = shotType;
        return packet;
    }

    public static ShotInfoPacket FromShot(Vector3 shotPosition, Vector3 shotDirection, MongoID ammoTemplate, float overheat,
        float lastShotOverheat, float lastShotTime, float durability, int chamberIndex, bool underbarrelShot,
        bool slideOnOverheatReached, EShotType shotType)
    {
        var packet = FirearmSubPacketPoolManager.Instance.GetPacket<ShotInfoPacket>(EFirearmSubPacketType.ShotInfo);
        packet.ShotPosition = shotPosition;
        packet.ShotDirection = shotDirection;
        packet.AmmoTemplate = ammoTemplate;
        packet.Overheat = overheat;
        packet.LastShotOverheat = lastShotOverheat;
        packet.LastShotTime = lastShotTime;
        packet.Durability = durability;
        packet.ChamberIndex = chamberIndex;
        packet.UnderbarrelShot = underbarrelShot;
        packet.SlideOnOverheatReached = slideOnOverheatReached;
        packet.ShotType = shotType;
        return packet;
    }

    public void Execute(FikaPlayer player)
    {
        if (!player.HealthController.IsAlive)
        {
            FikaGlobals.LogError("ShotInfoPacket::Execute: Player was not alive, can not process!");
            return;
        }

        if (player.HandsController is ObservedFirearmController controller)
        {
            controller.HandleShotInfoPacket(this, player.InventoryController);
        }
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.PutEnum(ShotType);
        if (ShotType == EShotType.DryFire)
        {
            writer.PutPackedInt(ChamberIndex, 0, 16);
            writer.Put(UnderbarrelShot);
            return;
        }

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

    public void Deserialize(NetDataReader reader)
    {
        ShotType = reader.GetEnum<EShotType>();
        if (ShotType == EShotType.DryFire)
        {
            ChamberIndex = reader.GetPackedInt(0, 16);
            UnderbarrelShot = reader.GetBool();
            return;
        }

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

    public void Dispose()
    {
        ShotPosition = default;
        ShotDirection = default;
        AmmoTemplate = default;
        Overheat = 0f;
        LastShotOverheat = 0f;
        LastShotTime = 0f;
        Durability = 0f;
        ChamberIndex = 0;
        UnderbarrelShot = false;
        SlideOnOverheatReached = false;
        ShotType = default;
    }
}
