using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;
using static EFT.Player;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public class CompassChangePacket : IPoolSubPacket
{
    private CompassChangePacket()
    {

    }

    public static CompassChangePacket FromValue(bool enabled)
    {
        CompassChangePacket packet = FirearmSubPacketPoolManager.Instance.GetPacket<CompassChangePacket>(EFirearmSubPacketType.CompassChange);
        packet.Enabled = enabled;
        return packet;
    }

    public static CompassChangePacket CreateInstance()
    {
        return new();
    }

    public bool Enabled;

    public void Execute(FikaPlayer player)
    {
        if (player.HandsController is ItemHandsController handsController)
        {
            handsController.CompassState.Value = Enabled;
        }
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Enabled);
    }

    public void Deserialize(NetDataReader reader)
    {
        Enabled = reader.GetBool();
    }

    public void Dispose()
    {
        Enabled = false;
    }
}
