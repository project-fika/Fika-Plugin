using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public readonly struct RocketShotPacket : IFirearmPacket
{
    public RocketShotPacket(Vector3 shotPosition, Vector3 shotForward, MongoID ammoTemplate)
    {
        ShotPosition = shotPosition;
        ShotForward = shotForward;
        AmmoTemplateId = ammoTemplate;
    }

    public RocketShotPacket(NetDataReader reader)
    {
        ShotPosition = reader.GetUnmanaged<Vector3>();
        ShotForward = reader.GetUnmanaged<Vector3>();
        AmmoTemplateId = reader.GetMongoID();
    }

    public readonly Vector3 ShotPosition;
    public readonly Vector3 ShotForward;
    public readonly MongoID AmmoTemplateId;

    public readonly void Execute(FikaPlayer player)
    {
        if (player.HandsController is ObservedFirearmController controller)
        {
            var rocketClass = (Ammo)Singleton<ItemFactory>.Instance.CreateItem(MongoID.Generate(), AmmoTemplateId, null);
            controller.HandleRocketShot(rocketClass, ShotPosition, ShotForward);
        }
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.PutUnmanaged(ShotPosition);
        writer.PutUnmanaged(ShotForward);
        writer.PutMongoID(AmmoTemplateId);
    }

    public EFirearmPacketType Type => EFirearmPacketType.RocketShot;
}
