// © 2026 Lacyway All Rights Reserved

using System;
using Il2CppInterop.Runtime;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using EFT.NetworkPackets;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Il2CppInterop.Runtime.Injection;

namespace Fika.Core.Main.ObservedClasses.HandsControllers;

internal sealed class ObservedMedsController : Player.MedsController
{
    public ObservedMedsController(IntPtr pointer) : base(pointer)
    {
    }

    private FikaPlayer _fikaPlayer;
    private int _animation;

    private ObservedMedsOperation ObservedOperation
    {
        get
        {
            return CurrentHandsOperation as ObservedMedsOperation;
        }
    }

    public static ObservedMedsController Create(FikaPlayer player, Item item, OneAndList<EBodyPart> bodyParts, float amount, int animationVariant)
    {
        var controller = CreateController<ObservedMedsController>(player, item, bodyParts, amount, animationVariant);
        controller.OnOutUseEventField = null;
        controller._fikaPlayer = player;
        controller._animation = animationVariant;
        return controller;
    }

    public override Il2CppSystem.Collections.Generic.Dictionary<Il2CppSystem.Type, OperationFactoryDelegate> GetOperationFactoryDelegates()
    {
        var operationFactoryDelegates = new Il2CppSystem.Collections.Generic.Dictionary<Il2CppSystem.Type, OperationFactoryDelegate>();
        operationFactoryDelegates[Il2CppType.Of<MedsInHandsOperation>()] = new System.Func<Player.ObjectInHandsOperation>(GetObservedMedsOperation);
        operationFactoryDelegates[Il2CppType.Of<ObservedMedsOperation>()] = new System.Func<Player.ObjectInHandsOperation>(GetObservedMedsOperation);
        return operationFactoryDelegates;
    }

    public override void Spawn(float animationSpeed, Il2CppSystem.Action callback)
    {
        FirearmsAnimator.SetAnimationSpeed(animationSpeed);
        FirearmsAnimator.SetPointOfViewOnSpawn(EPointOfView.ThirdPerson);
        InitiateOperation<ObservedMedsOperation>().ObservedStart(callback);
    }

    public override void Destroy()
    {
        if (ObservedOperation != null)
        {
            _fikaPlayer.HealthController.EffectRemovedEvent -= ObservedOperation.HealthController_EffectRemovedEvent;
            OnOutUseEvent -= ObservedOperation.ObservedMedsController_OnOutUseEvent;
        }
        base.Destroy();
    }

    public override void OnPlayerDead()
    {
        if (ObservedOperation != null)
        {
            _fikaPlayer.HealthController.EffectRemovedEvent -= ObservedOperation.HealthController_EffectRemovedEvent;
            OnOutUseEvent -= ObservedOperation.ObservedMedsController_OnOutUseEvent;
        }
        base.OnPlayerDead();
    }

    public override void Drop(float animationSpeed, Il2CppSystem.Action callback, bool fastDrop = false, Item nextControllerItem = null)
    {
        DropController().Forget();
    }

    private async Task DropController()
    {
        var operation = ObservedOperation;
        operation?.RequestDestroy();
        await Task.Delay(600);
        Destroyed = true;
        operation?.HideObservedWeapon();
    }

    private Player.ObjectInHandsOperation GetObservedMedsOperation()
    {
        return new ObservedMedsOperation(this);
    }

    public override bool CanChangeCompassState(bool newState)
    {
        return false;
    }

    public override void OnCanUsePropChanged(bool canUse)
    {
        // Do nothing
    }

    public override void SetCompassState(bool active)
    {
        // Do nothing
    }

    public override void FastForwardCurrentState()
    {
        ObservedOperation.FastForwardObserved();
    }

    public override void FastForwardCurrentOutdatedState()
    {
        var operation = ObservedOperation;

        if (operation != null && operation.IsOutdate())
        {
            operation.FastForwardObserved();
        }
    }

    public override void IEventsConsumerOnWeapOut()
    {
        ObservedOperation.HideObservedWeaponComplete();
    }

    private sealed class ObservedMedsOperation : Player.MedsController.MedsInHandsOperation
    {
        public ObservedMedsOperation(IntPtr pointer) : base(pointer)
        {
        }

        public ObservedMedsOperation(Player.MedsController controller) : base(Il2CppInjection.Allocate<ObservedMedsOperation>())
        {
            ClassInjector.DerivedConstructorBody(this);
            ClassInjector.InvokeBaseConstructor<Player.MedsController.MedsInHandsOperation>(this, controller);
            _observedMedsController = (ObservedMedsController)controller;
        }

        private readonly ObservedMedsController _observedMedsController;
        private int _animation;
        private bool _destroyRequested;

        public void ObservedStart(Il2CppSystem.Action callback)
        {
            State = Player.EOperationState.Executing;
            SetLeftStanceAnimOnStartOperation();
            callback?.Invoke();
            _animation = _observedMedsController._animation;
            ObservedMedsController_OnOutUseEvent();
            _observedMedsController.FirearmsAnimator.SetAnimationVariant(_animation);
            _observedMedsController._fikaPlayer.HealthController.EffectRemovedEvent += HealthController_EffectRemovedEvent;
            _observedMedsController.OnOutUseEvent += ObservedMedsController_OnOutUseEvent;
        }

        public void ObservedMedsController_OnOutUseEvent()
        {
            if (_observedMedsController.FirearmsAnimator != null)
            {
                _observedMedsController.FirearmsAnimator.SetActiveParam(true, false);
                _observedMedsController.FirearmsAnimator.SetNextLimb(false);
            }
        }

        public void HealthController_EffectRemovedEvent(IHealthEffect effect)
        {
            if (effect is not IMedEffect)
            {
                return;
            }

            if (_destroyRequested)
            {
                return;
            }

            if (_observedMedsController._player.HealthController.GetBodyPartHealth(EBodyPart.Common).AtMaximum)
            {
                return;
            }

            if (_observedMedsController.FirearmsAnimator != null)
            {
                var animator = _observedMedsController.FirearmsAnimator;

                if (animator.HasNextLimb())
                {
                    animator.SetActiveParam(false, false);
                    animator.SetNextLimb(true);
                }

                var mult = _observedMedsController._fikaPlayer.Skills.SurgerySpeed.Value / 100f;
                animator.SetUseTimeMultiplier(1f + mult);

                _animation++;
                var variant = 0;
                if (_observedMedsController.Item.TryGetItemComponent(out AnimationVariantsComponent animationVariantsComponent))
                {
                    variant = animationVariantsComponent.VariantsNumber;
                }
                var newAnim = (int)Mathf.Repeat(_animation, variant);
                animator.SetAnimationVariant(newAnim);
            }
        }

        public void HideObservedWeapon()
        {
            if (_observedMedsController != null && _observedMedsController.FirearmsAnimator != null)
            {
                _observedMedsController.FirearmsAnimator.SetNextLimb(false);
                _observedMedsController.FirearmsAnimator.SetActiveParam(false, true);
            }
        }

        public void HideObservedWeaponComplete()
        {
            State = Player.EOperationState.Finished;
        }

        public void FastForwardObserved()
        {
            if (State != Player.EOperationState.Finished)
            {
                HideObservedWeaponComplete();
            }
        }

        public void RequestDestroy()
        {
            _destroyRequested = true;
        }
    }
}
