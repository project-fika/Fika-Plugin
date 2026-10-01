using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public readonly struct LeftStanceChangePacket : IFirearmPacket
{
    public LeftStanceChangePacket(bool leftStance)
    {
        LeftStance = leftStance;
    }

    public LeftStanceChangePacket(NetDataReader reader)
    {
        LeftStance = reader.GetBool();
    }

    public readonly bool LeftStance;

    public readonly void Execute(FikaPlayer player)
    {
        if (player.HandsController is ObservedFirearmController controller)
        {
            if (player.MovementContext.LeftStanceEnabled != LeftStance)
            {
                controller.ChangeLeftStance();
            }
        }
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(LeftStance);
    }

    public EFirearmPacketType Type => EFirearmPacketType.LeftStanceChange;
}
