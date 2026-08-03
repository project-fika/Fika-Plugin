using System;
using System.Collections.Generic;
using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;

namespace Fika.Core.Main.ObservedClasses.HandsControllers;

public sealed class ObservedQuickUseItemController : Player.QuickUseItemController
{
    public static ObservedQuickUseItemController Create(ObservedPlayer player, Item item)
    {
        return CreateController<ObservedQuickUseItemController>(player, item);
    }

    public override Dictionary<Type, OperationFactoryDelegate> GetOperationFactoryDelegates()
    {
        return new Dictionary<Type, OperationFactoryDelegate>
        {
            {
                typeof(Player.QuickUseItemController.QuickUseOperation),
                new OperationFactoryDelegate(CreateObservedQuickUseItemControllerOperation)
            }
        };
    }

    public Player.ObjectInHandsOperation CreateObservedQuickUseItemControllerOperation()
    {
        return new ObservedQuickUseItemControllerOperation(this);
    }

    public sealed class ObservedQuickUseItemControllerOperation(ObservedQuickUseItemController controller) : Player.QuickUseItemController.QuickUseOperation(controller)
    {
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
                callback_(Controller);
            }
        }
    }
}
