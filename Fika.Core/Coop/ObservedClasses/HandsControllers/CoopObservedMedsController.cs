// © 2025 Lacyway All Rights Reserved

using EFT;
using EFT.InventoryLogic;
using Fika.Core.Coop.Players;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Fika.Core.Coop.ObservedClasses
{
    internal class CoopObservedMedsController : Player.MedsController
    {
        private CoopPlayer coopPlayer;
        private GStruct353<EBodyPart> healParts;

        private ObservedMedsOperation ObservedOperation
        {
            get
            {
                return CurrentHandsOperation as ObservedMedsOperation;
            }
        }

        public static CoopObservedMedsController Create(CoopPlayer player, Item item, GStruct353<EBodyPart> bodyParts, float amount, int animationVariant)
        {
            CoopObservedMedsController controller = smethod_6<CoopObservedMedsController>(player, item, bodyParts, amount, animationVariant);
            controller.coopPlayer = player;
            controller.healParts = bodyParts;
            return controller;
        }

        public override Dictionary<Type, OperationFactoryDelegate> GetOperationFactoryDelegates()
        {
            return new Dictionary<Type, OperationFactoryDelegate> {
                {
                    typeof(Class1172),
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
                coopPlayer.HealthController.EffectRemovedEvent -= ObservedOperation.HealthController_EffectRemovedEvent;
                OnOutUseEvent -= ObservedOperation.ObservedMedsController_OnOutUseEvent;
            }
            base.Destroy();
        }

        public override void OnPlayerDead()
        {
            if (ObservedOperation != null)
            {
                coopPlayer.HealthController.EffectRemovedEvent -= ObservedOperation.HealthController_EffectRemovedEvent;
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

        private class ObservedMedsOperation(Player.MedsController controller) : Class1172(controller)
        {
            private readonly CoopObservedMedsController observedMedsController = (CoopObservedMedsController)controller;
            private int animation;

            public void ObservedStart(Action callback)
            {
                State = Player.EOperationState.Executing;
                SetLeftStanceAnimOnStartOperation();
                callback();
                if (observedMedsController.Item.TryGetItemComponent(out AnimationVariantsComponent animationVariantsComponent))
                {
                    animation = UnityEngine.Random.Range(0, animationVariantsComponent.VariantsNumber);
                }
                else
                {
                    animation = 0;
                }
                observedMedsController.FirearmsAnimator.SetActiveParam(true, false);
                observedMedsController.coopPlayer.HealthController.EffectRemovedEvent += HealthController_EffectRemovedEvent;
                observedMedsController.OnOutUseEvent += ObservedMedsController_OnOutUseEvent;
            }

            public void ObservedMedsController_OnOutUseEvent()
            {
                if (observedMedsController.FirearmsAnimator != null)
                {
                    observedMedsController.FirearmsAnimator.SetActiveParam(true, false);
                }
                if (observedMedsController.FirearmsAnimator != null) // Yes, this double check is intentional
                {
                    observedMedsController.FirearmsAnimator.SetNextLimb(false);
                }
            }

            public void HealthController_EffectRemovedEvent(IEffect effect)
            {
                if (effect is not GInterface350)
                {
                    return;
                }

                if (observedMedsController.FirearmsAnimator != null)
                {
                    FirearmsAnimator animator = observedMedsController.FirearmsAnimator;

                    animator.SetActiveParam(false, false);
                    if (animator.HasNextLimb())
                    {
                        animator.SetNextLimb(true);
                    }

                    float mult = observedMedsController.coopPlayer.Skills.SurgerySpeed.Value / 100f;
                    animator.SetUseTimeMultiplier(1f + mult);

                    int variant = 0;
                    animation++;
                    if (observedMedsController.Item.TryGetItemComponent(out AnimationVariantsComponent animationVariantsComponent))
                    {
                        variant = animationVariantsComponent.VariantsNumber;
                    }
                    int newAnim = (int)Mathf.Repeat((float)animation, (float)variant);

                    animator.SetAnimationVariant(newAnim);
                }
            }

            public void HideObservedWeapon()
            {
                if (observedMedsController != null && observedMedsController.FirearmsAnimator != null)
                {
                    observedMedsController.FirearmsAnimator.SetNextLimb(false);
                    observedMedsController.FirearmsAnimator.SetActiveParam(false, true);
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
        }
    }
}
