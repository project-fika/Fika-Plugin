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
        return CreateController<ObservedQuickKnifeController>(observerdPlayer, item);
    }

    public override Dictionary<Type, OperationFactoryDelegate> GetOperationFactoryDelegates()
    {
        return new Dictionary<Type, OperationFactoryDelegate> {
            {
                typeof(Player.QuickKnifeKickController.QuickKnifeKickOperation),
                new OperationFactoryDelegate(CreateObservedQuickKnifeOperation)
            }
        };
    }

    public Player.ObjectInHandsOperation CreateObservedQuickKnifeOperation()
    {
        return new ObservedQuickKnifeOperation(this);
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

    public sealed class ObservedQuickKnifeOperation(ObservedQuickKnifeController controller) : Player.QuickKnifeKickController.QuickKnifeKickOperation(controller)
    {
        public override void HideWeapon(Action onHidden, bool fastHide)
        {
            onHidden();
            if (_kickFinished)
            {
                onHidden();
                return;
            }
            _onControllerDestroyed = onHidden;
        }
    }
}
