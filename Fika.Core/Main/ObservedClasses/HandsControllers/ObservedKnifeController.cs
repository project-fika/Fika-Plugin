// © 2026 Lacyway All Rights Reserved

using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Il2CppSystems.Effects;
using System;
using Il2CppInterop.Runtime.Injection;

namespace Fika.Core.Main.ObservedClasses.HandsControllers;

internal class ObservedKnifeController : Player.KnifeController
{
    public ObservedKnifeController(IntPtr pointer) : base(pointer)
    {
    }

    protected ObservedPlayer _observedPlayer;

    public static ObservedKnifeController Create(ObservedPlayer observerdPlayer, KnifeComponent item)
    {
        var controller = CreateController<ObservedKnifeController>(observerdPlayer, item);
        controller._observedPlayer = observerdPlayer;
        return controller;
    }

    public override void CompassStateHandler(bool isActive)
    {
        /*_observedPlayer.CreateObservedCompass();
        _objectInHandsAnimator.ShowCompass(isActive);
        _observedPlayer.SetPropVisibility(isActive);*/
    }

    public override PlayerHitInfo ProcessHit(Player.KnifeRaycastHit hit, BallisticCollider ballisticCollider)
    {
#if DEBUG
        FikaGlobals.LogInfo($"Hit from observed knife controller: {hit.point:F2}, ballisticCollider: {(ballisticCollider != null ? ballisticCollider.HitType : "none")}");
#endif
        if (ballisticCollider != null)
        {
            Singleton<Effects>.Instance.EffectsCommutator.PlayKnifeHitEffect(new DamageInfo
            {
                HitPoint = hit.point,
                HitNormal = hit.normal,
                Weapon = Knife.Item,
                HittedBallisticCollider = ballisticCollider
            });
        }

        return new PlayerHitInfo()
        {
            PoV = EPointOfView.ThirdPerson,
            Material = MaterialType.Body
        };
    }
}
