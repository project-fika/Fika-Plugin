using System;
using Il2CppInterop.Runtime;
using System.Collections.Generic;
using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using Il2CppInterop.Runtime.Injection;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.ObservedClasses.HandsControllers;

public sealed class ObservedQuickUseItemController : Player.QuickUseItemController
{
    public ObservedQuickUseItemController(IntPtr pointer) : base(pointer)
    {
    }

    public static ObservedQuickUseItemController Create(ObservedPlayer player, Item item)
    {
        return CreateController<ObservedQuickUseItemController>(player, item);
    }

    public override Il2CppSystem.Collections.Generic.Dictionary<Il2CppSystem.Type, OperationFactoryDelegate> GetOperationFactoryDelegates()
    {
        var operationFactoryDelegates = new Il2CppSystem.Collections.Generic.Dictionary<Il2CppSystem.Type, OperationFactoryDelegate>();
        operationFactoryDelegates[Il2CppType.Of<Player.QuickUseItemController.QuickUseOperation>()] = new System.Func<Player.ObjectInHandsOperation>(CreateObservedQuickUseItemControllerOperation);
        return operationFactoryDelegates;
    }

    public Player.ObjectInHandsOperation CreateObservedQuickUseItemControllerOperation()
    {
        return new ObservedQuickUseItemControllerOperation(this);
    }

    public sealed class ObservedQuickUseItemControllerOperation : Player.QuickUseItemController.QuickUseOperation
    {
        public ObservedQuickUseItemControllerOperation(IntPtr pointer) : base(pointer)
        {
        }

        public ObservedQuickUseItemControllerOperation(ObservedQuickUseItemController controller) : base(Il2CppInjection.Allocate<ObservedQuickUseItemControllerOperation>())
        {
            ClassInjector.DerivedConstructorBody(this);
            ClassInjector.InvokeBaseConstructor<Player.QuickUseItemController.QuickUseOperation>(this, controller);
        }

        /// <summary>
        /// Used to prevent nullref due to BSG never assigning _onControllerDestroyed
        /// </summary>
        public override void OnUseAction()
        {
            Controller.FirearmsAnimator.SetActiveParam(false, true);
            Controller.RemoveItemFromHand();
            if (Controller.Destroyed)
            {
                return;
            }
            if (_onUseCallback != null)
            {
                var callback_ = _onUseCallback;
                _onUseCallback = null;
                callback_.Invoke(Controller);
            }
        }
    }
}
