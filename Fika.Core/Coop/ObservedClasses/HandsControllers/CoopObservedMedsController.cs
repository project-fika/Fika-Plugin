// © 2024 Lacyway All Rights Reserved

using EFT;
using EFT.InventoryLogic;
using Fika.Core.Coop.Players;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fika.Core.Coop.ObservedClasses
{
	internal class CoopObservedMedsController : Player.MedsController
	{
		private CoopPlayer coopPlayer;

		private ObservedMedsOperation ObservedObsOperation
		{
			get
			{
				return CurrentHandsOperation as ObservedMedsOperation;
			}
		}

		public static CoopObservedMedsController Create(CoopPlayer player, Item item, EBodyPart bodyPart, float amount, int animationVariant)
		{
			CoopObservedMedsController controller = smethod_6<CoopObservedMedsController>(player, item, bodyPart, amount, animationVariant);
			controller.coopPlayer = player;
			return controller;
		}

		public override Dictionary<Type, OperationFactoryDelegate> GetOperationFactoryDelegates()
		{
			return new Dictionary<Type, OperationFactoryDelegate> {
				{
					typeof(Class1158),
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

		public override void Drop(float animationSpeed, Action callback, bool fastDrop = false, Item nextControllerItem = null)
		{
			DropController().HandleExceptions();
		}

		private async Task DropController()
		{
			await Task.Delay(600);
			Destroyed = true;
			ObservedObsOperation.HideObservedWeapon();
		}

		private Player.BaseAnimationOperation GetObservedMedsOperation()
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
			ObservedObsOperation.FastForwardObserved();
		}

		public override void IEventsConsumerOnWeapOut()
		{
			ObservedObsOperation.HideObservedWeaponComplete();
		}

		private class ObservedMedsOperation(Player.MedsController controller) : Class1158(controller)
		{
			private readonly CoopObservedMedsController observedMedsController = (CoopObservedMedsController)controller;

			public void ObservedStart(Action callback)
			{
				State = Player.EOperationState.Executing;
				SetLeftStanceAnimOnStartOperation();
				callback();
				observedMedsController.FirearmsAnimator.SetActiveParam(true, false);
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
