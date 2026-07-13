// © 2026 Lacyway All Rights Reserved

using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.InventoryLogic;
using EFT.NetworkPackets;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.World;

namespace Fika.Core.Main.ClientClasses.HandsControllers;

public class FikaClientQuickKnifeController : Player.QuickKnifeKickController
{
    protected FikaPlayer _fikaPlayer;

    public static FikaClientQuickKnifeController Create(FikaPlayer player, KnifeComponent item)
    {
        var controller = smethod_9<FikaClientQuickKnifeController>(player, item);
        controller._fikaPlayer = player;
        return controller;
    }

    public override ShotInfoClass vmethod_0(Player.GStruct182 hit, BallisticCollider ballisticCollider)
    {
        if (FikaBackendUtils.IsServer)
        {
            return base.vmethod_0(hit, ballisticCollider);
        }

        var shotInfo = base.vmethod_0(hit, ballisticCollider);
        if (ballisticCollider == null || ballisticCollider.HitType == EHitType.Default)
        {
            return shotInfo;
        }

        var packet = new KnifeHitPacket
        {
            NetId = _fikaPlayer.NetId,
            HitType = ballisticCollider.HitType,
            HitId = ballisticCollider.NetId,
            HitPoint = hit.point
        };
        Singleton<FikaClient>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered);

        return shotInfo;
    }
}
