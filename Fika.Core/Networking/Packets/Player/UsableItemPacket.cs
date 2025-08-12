using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.Player;

public class UsableItemPacket : IPoolSubPacket
{
    private UsableItemPacket() { }

    public static UsableItemPacket CreateInstance()
    {
        return new();
    }

    public static UsableItemPacket FromValue(bool hasCompassState, bool compassState, bool examineWeapon, bool hasAim, bool aimState)
    {
        UsableItemPacket packet = CommonSubPacketPoolManager.Instance.GetPacket<UsableItemPacket>(ECommonSubPacketType.UsableItem);
        packet.HasCompassState = hasCompassState;
        packet.CompassState = compassState;
        packet.ExamineWeapon = examineWeapon;
        packet.HasAim = hasAim;
        packet.AimState = aimState;
        return packet;
    }

    public bool HasCompassState;
    public bool CompassState;
    public bool ExamineWeapon;
    public bool HasAim;
    public bool AimState;

    public void Execute(FikaPlayer player = null)
    {
        player.HandleUsableItemPacket(this);
    }

    public void Deserialize(NetDataReader reader)
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

    public void Serialize(NetDataWriter writer)
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

    public void Dispose()
    {
        HasCompassState = false;
        CompassState = false;
        ExamineWeapon = false;
        HasAim = false;
        AimState = false;
    }
}
