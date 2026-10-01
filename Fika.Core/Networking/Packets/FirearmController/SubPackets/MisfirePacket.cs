using EFT;
using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public readonly struct MisfirePacket : IFirearmPacket
{
    public readonly MongoID AmmoTemplate;
    public readonly float Overheat;
    public readonly EShotType ShotType;

    public MisfirePacket(MongoID ammoTemplate, float overheat, EShotType shotType)
    {
        ShotType = shotType;
        AmmoTemplate = ammoTemplate;
        Overheat = overheat;
    }

    public MisfirePacket(NetDataReader reader)
    {
        ShotType = reader.GetEnum<EShotType>();
        AmmoTemplate = reader.GetMongoID();
        Overheat = reader.GetPackedFloat(0f, 200f, EFloatCompression.High);
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
            controller.HandleObservedMisfire(in this);
        }
        else
        {
            FikaGlobals.LogError($"ShotInfoPacket::Execute: HandsController was not ObservedFirearmController, was: {player.HandsController.GetType().Name}");
        }
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.PutEnum(ShotType);
        writer.PutMongoID(AmmoTemplate);
        writer.PutPackedFloat(Overheat, 0f, 200f, EFloatCompression.High);
    }

    public EFirearmPacketType Type => EFirearmPacketType.Misfire;
}
