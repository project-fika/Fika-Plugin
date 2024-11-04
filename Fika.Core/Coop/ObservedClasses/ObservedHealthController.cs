// © 2024 Lacyway All Rights Reserved

using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using Fika.Core.Coop.Players;
using System;
using System.Collections.Generic;

namespace Fika.Core.Coop.ObservedClasses
{
	public sealed class ObservedHealthController(byte[] serializedState, ObservedCoopPlayer player, InventoryController inventory, SkillManager skills)
		: NetworkHealthControllerAbstractClass(serializedState, inventory, skills)
	{
		public override Player Player
		{
			get
			{
				return player;
			}
		}

		public override bool ApplyItem(Item item, EBodyPart bodyPart, float? amount = null)
		{
			return false;
		}

		public override void CancelApplyingItem()
		{
			// Do nothing
		}

		public void PauseAllEffects()
		{
			EffectHandler handler = new(this);
			for (int i = list_1.Count - 1; i >= 0; i--)
			{
				handler.PausedEffects.Add(list_1[i]);
				PausedEffectsStruct gstruct = gclass803_0.Withdraw();
				gstruct.SaveInfo(list_1[i].Id, list_1[i].HealthController, list_1[i].Type, list_1[i].BodyPart, list_1[i].Strength,
					list_1[i].CurrentStrength, list_1[i].DelayTime, list_1[i].StateTime, list_1[i].WorkStateTime, list_1[i].BuildUpTime,
					list_1[i].ResidueTime, list_1[i].State);
				method_0(list_1[i]);
				list_1[i].ForceRemove();
				handler.PausedEffectsInfo.Add(gstruct);
			}
			action_2 = new Action(handler.UnpauseEffects);
		}

		public override Profile.ProfileHealthClass Store(Profile.ProfileHealthClass health = null)
		{
			Profile.ProfileHealthClass profileHealthClass;
			if ((profileHealthClass = health) == null)
			{
				Profile.ProfileHealthClass profileHealthClass2 = new()
				{
					BodyParts = GClass834<EBodyPart>.GetDictWith<Profile.ProfileHealthClass.GClass1940>(),
					Energy = new Profile.ProfileHealthClass.ValueInfo
					{
						Current = healthValue_0.Current,
						Minimum = healthValue_0.Minimum,
						Maximum = healthValue_0.Maximum
					},
					Hydration = new Profile.ProfileHealthClass.ValueInfo
					{
						Current = healthValue_1.Current,
						Minimum = healthValue_1.Minimum,
						Maximum = healthValue_1.Maximum
					},
					Temperature = new Profile.ProfileHealthClass.ValueInfo
					{
						Current = healthValue_2.Current,
						Minimum = healthValue_2.Minimum,
						Maximum = healthValue_2.Maximum
					}
				};
				profileHealthClass = profileHealthClass2;
				profileHealthClass2.Poison = new Profile.ProfileHealthClass.ValueInfo
				{
					Current = healthValue_3.Current,
					Minimum = healthValue_3.Minimum,
					Maximum = healthValue_3.Maximum
				};
			}
			health = profileHealthClass;
			foreach (KeyValuePair<EBodyPart, BodyPartState> keyValuePair in Dictionary_0)
			{
				keyValuePair.Deconstruct(out EBodyPart ebodyPart, out BodyPartState bodyPartState);
				EBodyPart ebodyPart2 = ebodyPart;
				BodyPartState bodyPartState2 = bodyPartState;
				if (!health.BodyParts.TryGetValue(ebodyPart2, out Profile.ProfileHealthClass.GClass1940 gclass))
				{
					gclass = new Profile.ProfileHealthClass.GClass1940();
					health.BodyParts.Add(ebodyPart2, gclass);
				}
				gclass.Health = new Profile.ProfileHealthClass.ValueInfo
				{
					Current = bodyPartState2.Health.Current,
					Maximum = bodyPartState2.Health.Maximum
				};
				gclass.Effects ??= [];
			}

			foreach (NetworkBodyEffectsAbstractClass gclass in IReadOnlyList_0)
			{
				if (gclass is GInterface295 && gclass.State != EEffectState.Residued) // We only resync effects that are in-game effects, check for GClass increments
				{
					Profile.ProfileHealthClass.GClass1940 gclass2 = health.BodyParts[gclass.BodyPart];
					gclass2.Effects ??= [];
					gclass2.Effects.Add(gclass.GetType().Name, new()
					{
						Time = gclass.TimeLeft
					});
				}
			}

			return health;
		}

		private class EffectHandler(ObservedHealthController healthController)
		{
			private readonly ObservedHealthController healthController = healthController;
			public readonly List<NetworkBodyEffectsAbstractClass> PausedEffects = [];
			public readonly List<PausedEffectsStruct> PausedEffectsInfo = [];

			public void UnpauseEffects()
			{
				for (int i = PausedEffects.Count - 1; i >= 0; i--)
				{
					PausedEffects[i].SetEffectInfo(PausedEffectsInfo[i].ID, PausedEffectsInfo[i].HealthController, PausedEffectsInfo[i].Type,
						PausedEffectsInfo[i].BodyPart, PausedEffectsInfo[i].Strength, PausedEffectsInfo[i].CurrentStrength,
						PausedEffectsInfo[i].DelayTime, PausedEffectsInfo[i].StateTime, PausedEffectsInfo[i].WorkStateTime,
						PausedEffectsInfo[i].BuildUpTime, PausedEffectsInfo[i].ResidueStateTime, PausedEffectsInfo[i].State);
					healthController.AddEffectToList(PausedEffects[i]);
					PausedEffects[i].UnPauseEffect();
					healthController.gclass803_0.Return(PausedEffectsInfo[i]);
				}
			}
		}
	}
}
