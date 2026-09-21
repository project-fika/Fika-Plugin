// © 2026 Lacyway All Rights Reserved

using System;
using Il2CppInterop.Runtime;
using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Il2CppSystems.Effects;
using Il2CppInterop.Runtime.Injection;

namespace Fika.Core.Main.ObservedClasses.HandsControllers;

internal sealed class ObservedQuickKnifeController : Player.QuickKnifeKickController
{
    public ObservedQuickKnifeController(IntPtr pointer) : base(pointer)
    {
    }

    public static ObservedQuickKnifeController Create(ObservedPlayer observerdPlayer, KnifeComponent item)
    {
        return CreateController<ObservedQuickKnifeController>(observerdPlayer, item);
    }

    public override Il2CppSystem.Collections.Generic.Dictionary<Il2CppSystem.Type, OperationFactoryDelegate> GetOperationFactoryDelegates()
    {
        var operationFactoryDelegates = new Il2CppSystem.Collections.Generic.Dictionary<Il2CppSystem.Type, OperationFactoryDelegate>();
        operationFactoryDelegates[Il2CppType.Of<Player.QuickKnifeKickController.QuickKnifeKickOperation>()] = new System.Func<Player.ObjectInHandsOperation>(CreateObservedQuickKnifeOperation);
        return operationFactoryDelegates;
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

    public sealed class ObservedQuickKnifeOperation : Player.QuickKnifeKickController.QuickKnifeKickOperation
    {
        public ObservedQuickKnifeOperation(IntPtr pointer) : base(pointer)
        {
        }

        public ObservedQuickKnifeOperation(ObservedQuickKnifeController controller) : base(Il2CppInjection.Allocate<ObservedQuickKnifeOperation>())
        {
            ClassInjector.DerivedConstructorBody(this);
            ClassInjector.InvokeBaseConstructor<Player.QuickKnifeKickController.QuickKnifeKickOperation>(this, controller);
        }

        public override void HideWeapon(Il2CppSystem.Action onHidden, bool fastHide)
        {
            onHidden.Invoke();
            if (_kickFinished)
            {
                onHidden.Invoke();
                return;
            }
            _onControllerDestroyed = onHidden;
        }
    }
}
