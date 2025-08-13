using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.FirearmController;

public class LeftStanceChangePacket : IPoolSubPacket
{
    private LeftStanceChangePacket()
    {

    }

    public static LeftStanceChangePacket FromValue(bool leftStance)
    {
        LeftStanceChangePacket packet = FirearmSubPacketPoolManager.Instance.GetPacket<LeftStanceChangePacket>(EFirearmSubPacketType.LeftStanceChange);
        packet.LeftStance = leftStance;
        return packet;
    }

    public static LeftStanceChangePacket CreateInstance()
    {
        return new();
    }

    public bool LeftStance;

    public void Execute(FikaPlayer player)
    {
        if (player.HandsController is ObservedFirearmController controller)
        {
            if (player.MovementContext.LeftStanceEnabled != LeftStance)
            {
                controller.ChangeLeftStance();
            }
        }
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(LeftStance);
    }

    public void Deserialize(NetDataReader reader)
    {
        LeftStance = reader.GetBool();
    }

    public void Dispose()
    {
        LeftStance = false;
    }
}
