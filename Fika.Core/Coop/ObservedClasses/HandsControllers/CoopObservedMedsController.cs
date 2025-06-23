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
        private CoopPlayer _coopPlayer;
        private GStruct375<EBodyPart> _healParts;

        private ObservedMedsOperation ObservedOperation
        {
            get
            {
                return CurrentHandsOperation as ObservedMedsOperation;
            }
        }

        public static CoopObservedMedsController Create(CoopPlayer player, Item item, GStruct375<EBodyPart> bodyParts, float amount, int animationVariant)
        {
            CoopObservedMedsController controller = smethod_6<CoopObservedMedsController>(player, item, bodyParts, amount, animationVariant);
            controller._coopPlayer = player;
            controller._healParts = bodyParts;
            return controller;
        }

        public override Dictionary<Type, OperationFactoryDelegate> GetOperationFactoryDelegates()
        {
            return new Dictionary<Type, OperationFactoryDelegate> {
                {
                    typeof(Class1197),
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
                _coopPlayer.HealthController.EffectRemovedEvent -= ObservedOperation.HealthController_EffectRemovedEvent;
                OnOutUseEvent -= ObservedOperation.ObservedMedsController_OnOutUseEvent;
            }
            base.Destroy();
        }

        public override void OnPlayerDead()
        {
            if (ObservedOperation != null)
            {
                _coopPlayer.HealthController.EffectRemovedEvent -= ObservedOperation.HealthController_EffectRemovedEvent;
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

        private class ObservedMedsOperation(Player.MedsController controller) : Class1197(controller)
        {
            private readonly CoopObservedMedsController _observedMedsController = (CoopObservedMedsController)controller;
            private int _animation;
            private bool _destroyRequested;

            public void ObservedStart(Action callback)
            {
                State = Player.EOperationState.Executing;
                SetLeftStanceAnimOnStartOperation();
                callback();
                if (_observedMedsController.Item.TryGetItemComponent(out AnimationVariantsComponent animationVariantsComponent))
                {
                    _animation = UnityEngine.Random.Range(0, animationVariantsComponent.VariantsNumber);
                }
                else
                {
                    _animation = 0;
                }
                _observedMedsController.FirearmsAnimator.SetActiveParam(true, false);
                _observedMedsController._coopPlayer.HealthController.EffectRemovedEvent += HealthController_EffectRemovedEvent;
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
                if (effect is not GInterface354)
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
                    FirearmsAnimator animator = _observedMedsController.FirearmsAnimator;

                    animator.SetActiveParam(false, false);
                    if (animator.HasNextLimb())
                    {
                        animator.SetNextLimb(true);
                    }

                    float mult = _observedMedsController._coopPlayer.Skills.SurgerySpeed.Value / 100f;
                    animator.SetUseTimeMultiplier(1f + mult);

                    int variant = 0;
                    _animation++;
                    if (_observedMedsController.Item.TryGetItemComponent(out AnimationVariantsComponent animationVariantsComponent))
                    {
                        variant = animationVariantsComponent.VariantsNumber;
                    }
                    int newAnim = (int)Mathf.Repeat((float)_animation, (float)variant);

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
}
