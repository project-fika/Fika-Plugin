using Fika.Core.Main.Players;

namespace Fika.Core.Networking.Packets.Player.Common.SubPackets;

public readonly struct UsableItemPacket
{
    public UsableItemPacket(bool hasCompassState, bool compassState, bool examineWeapon, bool hasAim, bool aimState)
    {
        HasCompassState = hasCompassState;
        CompassState = compassState;
        ExamineWeapon = examineWeapon;
        HasAim = hasAim;
        AimState = aimState;
    }

    public UsableItemPacket(NetDataReader reader)
    {
        HasCompassState = reader.GetBool();
        if (HasCompassState)
        {
            CompassState = reader.GetBool();
        }
        ExamineWeapon = reader.GetBool();
        HasAim = reader.GetBool();
        if (HasAim)
        {
            AimState = reader.GetBool();
        }
    }

    public readonly bool HasCompassState;
    public readonly bool CompassState;
    public readonly bool ExamineWeapon;
    public readonly bool HasAim;
    public readonly bool AimState;

    public readonly void Execute(FikaPlayer player = null)
    {
        player.HandleUsableItemPacket(this);
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(HasCompassState);
        if (HasCompassState)
        {
            writer.Put(CompassState);
        }
        writer.Put(ExamineWeapon);
        writer.Put(HasAim);
        if (HasAim)
        {
            writer.Put(AimState);
        }
    }
}
