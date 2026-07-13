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
        return smethod_6<ObservedQuickUseItemController>(player, item);
    }

    public override Dictionary<Type, OperationFactoryDelegate> GetOperationFactoryDelegates()
    {
        return new Dictionary<Type, OperationFactoryDelegate>
        {
            {
                typeof(GClass2058),
                new OperationFactoryDelegate(CreateObservedQuickUseItemControllerOperation)
            }
        };
    }

    public Player.BaseAnimationOperationClass CreateObservedQuickUseItemControllerOperation()
    {
        return new ObservedQuickUseItemControllerOperation(this);
    }

    public sealed class ObservedQuickUseItemControllerOperation(ObservedQuickUseItemController controller) : GClass2058(controller)
    {
        /// <summary>
        /// Used to prevent nullref due to BSG never assigning Action_1
        /// </summary>
        public override void OnUseAction()
        {
            QuickUseItemController_0.FirearmsAnimator.SetActiveParam(false, true);
            QuickUseItemController_0.method_4();
            if (QuickUseItemController_0.Destroyed)
            {
                return;
            }
            if (Callback_0 != null)
            {
                var callback_ = Callback_0;
                Callback_0 = null;
                callback_(QuickUseItemController_0);
            }
        }
    }
}
