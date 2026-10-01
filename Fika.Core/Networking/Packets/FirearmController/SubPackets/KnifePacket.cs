using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public readonly struct KnifePacket : IFirearmPacket
{
    public KnifePacket(bool examine, bool kick, bool altKick, bool breakCombo)
    {
        Examine = examine;
        Kick = kick;
        AltKick = altKick;
        BreakCombo = breakCombo;
    }

    public KnifePacket(NetDataReader reader)
    {
        Examine = reader.GetBool();
        Kick = reader.GetBool();
        AltKick = reader.GetBool();
        BreakCombo = reader.GetBool();
    }

    public readonly bool Examine;
    public readonly bool Kick;
    public readonly bool AltKick;
    public readonly bool BreakCombo;

    public readonly void Execute(FikaPlayer player)
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

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(Examine);
        writer.Put(Kick);
        writer.Put(AltKick);
        writer.Put(BreakCombo);
    }

    public EFirearmPacketType Type => EFirearmPacketType.Knife;
}
