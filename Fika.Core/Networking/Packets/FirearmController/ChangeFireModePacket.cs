using EFT.InventoryLogic;
using Fika.Core.Main.ObservedClasses;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;
using LiteNetLib.Utils;

namespace Fika.Core.Networking.Packets.FirearmController;

public class ChangeFireModePacket : IPoolSubPacket
{
    private ChangeFireModePacket()
    {

    }

    public static ChangeFireModePacket FromValue(Weapon.EFireMode fireMode)
    {
        ChangeFireModePacket packet = FirearmSubPacketPoolManager.Instance.GetPacket<ChangeFireModePacket>(EFirearmSubPacketType.ChangeFireMode);
        packet.FireMode = fireMode;
        return packet;
    }

    public static ChangeFireModePacket CreateInstance()
    {
        return new();
    }

    public Weapon.EFireMode FireMode;

    public void Execute(FikaPlayer player)
    {
        if (player.HandsController is ObservedFirearmController controller)
        {
            controller.ChangeFireMode(FireMode);
        }
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.PutEnum(FireMode);
    }

    public void Deserialize(NetDataReader reader)
    {
        FireMode = reader.GetEnum<Weapon.EFireMode>();
    }

    public void Dispose()
    {
        FireMode = Weapon.EFireMode.fullauto;
    }
}
