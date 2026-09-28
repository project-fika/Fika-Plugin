using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public readonly struct DryShotPacket : IFirearmPacket
{
    public readonly int ChamberIndex;
    public readonly bool UnderbarrelShot;

    public DryShotPacket(int chamberIndex, bool underbarrelShot)
    {
        ChamberIndex = chamberIndex;
        UnderbarrelShot = underbarrelShot;
    }

    public DryShotPacket(NetDataReader reader)
    {
        ChamberIndex = reader.GetPackedInt(0, 16);
        UnderbarrelShot = reader.GetBool();
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
            controller.HandleObservedDryShot();
        }
        else
        {
            FikaGlobals.LogError($"ShotInfoPacket::Execute: HandsController was not ObservedFirearmController, was: {player.HandsController.GetType().Name}");
        }
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.PutPackedInt(ChamberIndex, 0, 16);
        writer.Put(UnderbarrelShot);
    }

    public EFirearmPacketType Type => EFirearmPacketType.DryShot;
}
