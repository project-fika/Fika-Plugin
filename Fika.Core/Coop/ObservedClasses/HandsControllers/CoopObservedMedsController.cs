// © 2025 Lacyway All Rights Reserved

using EFT;
using EFT.InventoryLogic;
using Fika.Core.Coop.Players;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fika.Core.Coop.ObservedClasses
{
    internal class CoopObservedMedsController : Player.MedsController
    {
        private CoopPlayer coopPlayer;

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
            coopPlayer.HealthController.EffectRemovedEvent -= ObservedOperation.HealthController_EffectRemovedEvent;
            Item.Owner.RemoveItemEvent -= ObservedOperation.Owner_RemoveItemEvent;
            base.Destroy();
        }

        public override void OnPlayerDead()
        {
            coopPlayer.HealthController.EffectRemovedEvent -= ObservedOperation.HealthController_EffectRemovedEvent;
            Item.Owner.RemoveItemEvent -= ObservedOperation.Owner_RemoveItemEvent;
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

            public void ObservedStart(Action callback)
            {
                State = Player.EOperationState.Executing;
                SetLeftStanceAnimOnStartOperation();
                callback();
                observedMedsController.FirearmsAnimator.SetActiveParam(true, false);
                observedMedsController.coopPlayer.HealthController.EffectRemovedEvent += HealthController_EffectRemovedEvent;
                observedMedsController.Item.Owner.RemoveItemEvent += Owner_RemoveItemEvent;
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
                    if (observedMedsController.Item.TryGetItemComponent(out AnimationVariantsComponent animationVariantsComponent))
                    {
                        int numVariants = animationVariantsComponent.VariantsNumber;
                        variant = UnityEngine.Random.Range(0, numVariants);
                    }

                    animator.SetAnimationVariant(variant);
                }
            }

            public override void OnEnd()
            {
                base.OnEnd();
            }

            public void Owner_RemoveItemEvent(GEventArgs3 args)
            {
                if (args.Item != observedMedsController.Item)
                {
                    return;
                }

                observedMedsController.coopPlayer.HealthController.EffectRemovedEvent -= HealthController_EffectRemovedEvent;
            }

            public void HideObservedWeapon()
            {
                if (observedMedsController != null && observedMedsController.FirearmsAnimator != null)
                {
                    observedMedsController.FirearmsAnimator.SetActiveParam(false, false);
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
