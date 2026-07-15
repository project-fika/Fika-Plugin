// © 2026 Lacyway All Rights Reserved

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.ObservedClasses.HandsControllers;

internal sealed class ObservedMedsController : Player.MedsController
{
    private FikaPlayer _fikaPlayer;
    private int _animation;

    private readonly static FieldInfo _onOutUseAction = typeof(Player.MedsController)
        .GetField("action_0", BindingFlags.NonPublic | BindingFlags.Instance);

    private ObservedMedsOperation ObservedOperation
    {
        get
        {
            return CurrentHandsOperation as ObservedMedsOperation;
        }
    }

    public static ObservedMedsController Create(FikaPlayer player, Item item, GStruct382<EBodyPart> bodyParts, float amount, int animationVariant)
    {
        var controller = smethod_6<ObservedMedsController>(player, item, bodyParts, amount, animationVariant);
        var action = (Action)_onOutUseAction.GetValue(controller);
        _onOutUseAction.SetValue(controller, FikaGlobals.ClearDelegates(action));
        controller._fikaPlayer = player;
        controller._animation = animationVariant;
        return controller;
    }

    public override Dictionary<Type, OperationFactoryDelegate> GetOperationFactoryDelegates()
    {
        return new Dictionary<Type, OperationFactoryDelegate> {
            {
                typeof(ObservedMedsControllerClass),
                new OperationFactoryDelegate(GetObservedMedsOperation)
            },
            {
                typeof(ObservedMedsOperation),
                new OperationFactoryDelegate(GetObservedMedsOperation)
            }
        };
    }

    public override void Spawn(float animationSpeed, Action callback)
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

    public override void Drop(float animationSpeed, Action callback, bool fastDrop = false, Item nextControllerItem = null)
    {
        DropController().HandleExceptions();
    }

    private async Task DropController()
    {
        if (ObservedOperation != null)
        {
            ObservedOperation.RequestDestroy();
        }
        await Task.Delay(600);
        Destroyed = true;
        ObservedOperation.HideObservedWeapon();
    }

    private Player.BaseAnimationOperationClass GetObservedMedsOperation()
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

    public override void IEventsConsumerOnWeapOut()
    {
        ObservedOperation.HideObservedWeaponComplete();
    }

    private sealed class ObservedMedsOperation(Player.MedsController controller) : ObservedMedsControllerClass(controller)
    {
        private readonly ObservedMedsController _observedMedsController = (ObservedMedsController)controller;
        private int _animation;
        private bool _destroyRequested;

        public void ObservedStart(Action callback)
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

        public void HealthController_EffectRemovedEvent(IEffect effect)
        {
            // Look for GClass increments
            if (effect is not GInterface376)
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
