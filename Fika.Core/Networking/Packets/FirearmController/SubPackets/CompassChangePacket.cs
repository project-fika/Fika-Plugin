using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public sealed class CompassChangePacket : IPoolSubPacket
{
    private CompassChangePacket()
    {

    }

    public static CompassChangePacket FromValue(bool enabled)
    {
        var packet = FirearmSubPacketPoolManager.Instance.GetPacket<CompassChangePacket>(EFirearmSubPacketType.CompassChange);
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
        // temporarily disabled, broken in base game
        /*if (player.HandsController is ItemHandsController handsController)
        {
            handsController.CompassStateHandler(Enabled);
        }*/
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
