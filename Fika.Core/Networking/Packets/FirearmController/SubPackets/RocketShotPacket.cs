using Comfort.Common;
using EFT;
using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public sealed class RocketShotPacket : IPoolSubPacket
{
    private RocketShotPacket()
    {

    }

    public static RocketShotPacket FromValue(Vector3 shotPosition, Vector3 shotForward, MongoID ammoTemplate)
    {
        var packet = FirearmSubPacketPoolManager.Instance.GetPacket<RocketShotPacket>(EFirearmSubPacketType.RocketShot);
        packet.ShotPosition = shotPosition;
        packet.ShotForward = shotForward;
        packet.AmmoTemplateId = ammoTemplate;
        return packet;
    }

    public static RocketShotPacket CreateInstance()
    {
        return new();
    }

    public Vector3 ShotPosition;
    public Vector3 ShotForward;
    public MongoID AmmoTemplateId;

    public void Execute(FikaPlayer player)
    {
        if (player.HandsController is ObservedFirearmController controller)
        {
            var rocketClass = (AmmoItemClass)Singleton<ItemFactoryClass>.Instance.CreateItem(MongoID.Generate(), AmmoTemplateId, null);
            controller.HandleRocketShot(rocketClass, ShotPosition, ShotForward);
        }
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.PutUnmanaged(ShotPosition);
        writer.PutUnmanaged(ShotForward);
        writer.PutMongoID(AmmoTemplateId);
    }

    public void Deserialize(NetDataReader reader)
    {
        ShotPosition = reader.GetUnmanaged<Vector3>();
        ShotForward = reader.GetUnmanaged<Vector3>();
        AmmoTemplateId = reader.GetMongoID();
    }

    public void Dispose()
    {
        ShotPosition = default;
        ShotForward = default;
        AmmoTemplateId = default;
    }
}
