// © 2026 Lacyway All Rights Reserved

using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Systems.Effects;

namespace Fika.Core.Main.ObservedClasses.HandsControllers;

internal class ObservedKnifeController : Player.KnifeController
{
    protected ObservedPlayer _observedPlayer;

    public static ObservedKnifeController Create(ObservedPlayer observerdPlayer, KnifeComponent item)
    {
        var controller = smethod_9<ObservedKnifeController>(observerdPlayer, item);
        controller._observedPlayer = observerdPlayer;
        return controller;
    }

    public override void CompassStateHandler(bool isActive)
    {
        /*_observedPlayer.CreateObservedCompass();
        _objectInHandsAnimator.ShowCompass(isActive);
        _observedPlayer.SetPropVisibility(isActive);*/
    }

    public override ShotInfoClass vmethod_0(Player.GStruct182 hit, BallisticCollider ballisticCollider)
    {
#if DEBUG
        FikaGlobals.LogInfo($"Hit from observed knife controller: {hit.point:F2}, ballisticCollider: {(ballisticCollider != null ? ballisticCollider.HitType : "none")}");
#endif
        if (ballisticCollider != null)
        {
            Singleton<Effects>.Instance.EffectsCommutator.PlayKnifeHitEffect(new DamageInfoStruct
            {
                HitPoint = hit.point,
                HitNormal = hit.normal,
                Weapon = Knife.Item,
                HittedBallisticCollider = ballisticCollider
            });
        }

        return new ShotInfoClass()
        {
            PoV = EPointOfView.ThirdPerson,
            Material = MaterialType.Body
        };
    }
}
