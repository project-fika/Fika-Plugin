// © 2024 Lacyway All Rights Reserved

using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using Fika.Core.Coop.Players;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fika.Core.Coop.ObservedClasses
{
	internal class CoopObservedMedsController : EFT.Player.MedsController
	{
		private CoopPlayer coopPlayer;
		private ObservedMedsOperation observedObsOperation
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
			}};
		}

		public override void Drop(float animationSpeed, Action callback, bool fastDrop = false, Item nextControllerItem = null)
		{
			DropController(callback).HandleExceptions();
		}

		private async Task DropController(Action callback)
		{
			await Task.Delay(600);
			Destroyed = true;
			observedObsOperation.HideObservedWeapon(callback);
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

		private class ObservedMedsOperation(Player.MedsController controller) : Class1158(controller)
		{
			private readonly CoopObservedMedsController observedMedsController = (CoopObservedMedsController)controller;
			private Action hiddenCallback;

			public void HideObservedWeapon(Action onHiddenCallback)
			{
				ActiveHealthController activeHealthController = observedMedsController._player.ActiveHealthController;
				if (activeHealthController != null)
				{
					activeHealthController.RemoveMedEffect();
				}
				observedMedsController._player.HealthController.EffectRemovedEvent -= method_2;
				hiddenCallback = onHiddenCallback;
				if (observedMedsController.FirearmsAnimator != null)
				{
					observedMedsController.FirearmsAnimator.SetActiveParam(false, false);
				}
				if (State == Player.EOperationState.Finished)
				{
					hiddenCallback();
				}
			}
		}
	}
}
