// © 2026 Lacyway All Rights Reserved

using System;
using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Systems.Effects;

namespace Fika.Core.Main.ObservedClasses.HandsControllers;

internal sealed class ObservedQuickKnifeController : Player.QuickKnifeKickController
{
    public static ObservedQuickKnifeController Create(ObservedPlayer observerdPlayer, KnifeComponent item)
    {
        return smethod_9<ObservedQuickKnifeController>(observerdPlayer, item);
    }

    public override Dictionary<Type, OperationFactoryDelegate> GetOperationFactoryDelegates()
    {
        return new Dictionary<Type, OperationFactoryDelegate> {
            {
                typeof(Class1290),
                new OperationFactoryDelegate(CreateObservedQuickKnifeOperation)
            }
        };
    }

    public Player.BaseAnimationOperationClass CreateObservedQuickKnifeOperation()
    {
        return new ObservedQuickKnifeOperation(this);
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

    public sealed class ObservedQuickKnifeOperation(ObservedQuickKnifeController controller) : Class1290(controller)
    {
        public override void HideWeapon(Action onHidden, bool fastHide)
        {
            onHidden();
            if (Bool_0)
            {
                onHidden();
                return;
            }
            Action_1 = onHidden;
        }
    }
}
