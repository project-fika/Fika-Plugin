// © 2026 Lacyway All Rights Reserved

using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Packets.FirearmController;
using Fika.Core.Networking.Packets.FirearmController.SubPackets;

namespace Fika.Core.Main.ClientClasses.HandsControllers;

/// <summary>
/// This is only used by AI
/// </summary>
public class FikaClientQuickGrenadeController : Player.QuickGrenadeThrowHandsController
{
    protected FikaPlayer _fikaPlayer;

    public static FikaClientQuickGrenadeController Create(FikaPlayer player, ThrowWeap item)
    {
        var controller = CreateController<FikaClientQuickGrenadeController>(player, item);
        controller._fikaPlayer = player;
        return controller;
    }

    public override void ThrowGrenade(float timeSinceSafetyLevelRemoved, Vector3 position, Quaternion rotation, Vector3 force, bool lowThrow)
    {
        var packet = new GrenadePacket(
            rotation,
            position,
            force,
            EGrenadePacketType.None,
            true,
            lowThrow,
            false,
            false,
            false
        );
        _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);

        base.ThrowGrenade(timeSinceSafetyLevelRemoved, position, rotation, force, lowThrow);
    }
}
