using EFT.InventoryLogic;
using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public readonly struct ChangeFireModePacket : IFirearmPacket
{
    public EFirearmPacketType Type => EFirearmPacketType.ChangeFireMode;

    public readonly Weapon.EFireMode FireMode;

    public ChangeFireModePacket(Weapon.EFireMode fireMode)
    {
        FireMode = fireMode;
    }

    public ChangeFireModePacket(NetDataReader reader)
    {
        FireMode = reader.GetEnum<Weapon.EFireMode>();
    }

    public readonly void Execute(FikaPlayer player)
    {
        if (player.HandsController is ObservedFirearmController controller)
        {
            controller.ChangeFireMode(FireMode);
        }
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.PutEnum(FireMode);
    }
}
