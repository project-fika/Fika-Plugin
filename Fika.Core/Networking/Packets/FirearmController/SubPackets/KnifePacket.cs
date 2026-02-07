using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public sealed class KnifePacket : IPoolSubPacket
{
    private KnifePacket()
    {

    }

    public static KnifePacket FromValue(bool examine, bool kick, bool altKick, bool breakCombo)
    {
        var packet = FirearmSubPacketPoolManager.Instance.GetPacket<KnifePacket>(EFirearmSubPacketType.Knife);
        packet.Examine = examine;
        packet.Kick = kick;
        packet.AltKick = altKick;
        packet.BreakCombo = breakCombo;
        return packet;
    }

    public static KnifePacket CreateInstance()
    {
        return new();
    }

    public bool Examine;
    public bool Kick;
    public bool AltKick;
    public bool BreakCombo;

    public void Execute(FikaPlayer player)
    {
        if (player.HandsController is ObservedKnifeController knifeController)
        {
            if (Examine)
            {
                knifeController.ExamineWeapon();
            }

            if (Kick)
            {
                knifeController.MakeKnifeKick();
            }

            if (AltKick)
            {
                knifeController.MakeAlternativeKick();
            }

            if (BreakCombo)
            {
                knifeController.BrakeCombo();
            }
        }
        else
        {
            FikaGlobals.LogError($"KnifePacket: HandsController was not of type CoopObservedKnifeController! Was {player.HandsController.GetType().Name}");
        }
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Examine);
        writer.Put(Kick);
        writer.Put(AltKick);
        writer.Put(BreakCombo);
    }

    public void Deserialize(NetDataReader reader)
    {
        Examine = reader.GetBool();
        Kick = reader.GetBool();
        AltKick = reader.GetBool();
        BreakCombo = reader.GetBool();
    }

    public void Dispose()
    {
        Examine = false;
        Kick = false;
        AltKick = false;
        BreakCombo = false;
    }
}
