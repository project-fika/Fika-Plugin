using Fika.Core.Main.Players;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public readonly struct CompassChangePacket : IFirearmPacket
{
    public EFirearmPacketType Type => EFirearmPacketType.CompassChange;

    public CompassChangePacket(bool enabled)
    {
        Enabled = enabled;
    }

    public CompassChangePacket(NetDataReader reader)
    {
        Enabled = reader.GetBool();
    }

    public readonly bool Enabled;

    public readonly void Execute(FikaPlayer player)
    {
        // temporarily disabled, broken in base game
        /*if (player.HandsController is ItemHandsController handsController)
        {
            handsController.CompassStateHandler(Enabled);
        }*/
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(Enabled);
    }
}
