using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Pooling;
using static EFT.Player;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public sealed class CompassChangePacket : IPoolSubPacket
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
#if DEBUG
        FikaGlobals.LogInfo("Received CompassState packet"); 
#endif
        if (player.HandsController is ItemHandsController handsController)
        {
#if DEBUG
            FikaGlobals.LogInfo("Executing CompassState packet"); 
#endif
            handsController.CompassStateHandler(Enabled);
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
