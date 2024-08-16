// © 2024 Lacyway All Rights Reserved

using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using System.Collections.Generic;

namespace Fika.Core.Coop.ObservedClasses
{
	public sealed class ObservedHealthController(byte[] serializedState, InventoryControllerClass inventory, SkillManager skills) : NetworkHealthControllerAbstractClass(serializedState, inventory, skills)
	{
		public override bool ApplyItem(Item item, EBodyPart bodyPart, float? amount = null)
		{
			return false;
		}

		public override void CancelApplyingItem()
		{
			// Do nothing
		}

		public override Profile.ProfileHealthClass Store(Profile.ProfileHealthClass health = null)
		{
			Profile.ProfileHealthClass profileHealthClass;
			if ((profileHealthClass = health) == null)
			{
				Profile.ProfileHealthClass profileHealthClass2 = new()
				{
					BodyParts = GClass769<EBodyPart>.GetDictWith<Profile.ProfileHealthClass.GClass1770>(),
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
				if (!health.BodyParts.TryGetValue(ebodyPart2, out Profile.ProfileHealthClass.GClass1770 gclass))
				{
					gclass = new Profile.ProfileHealthClass.GClass1770();
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
				if (gclass is GInterface251 && gclass.State != EEffectState.Residued) // We only resync effects that are in-game effects, check for GClass increments
				{
					Profile.ProfileHealthClass.GClass1770 gclass2 = health.BodyParts[gclass.BodyPart];
					gclass2.Effects ??= [];
					gclass2.Effects.Add(gclass.GetType().Name, new()
					{
						Time = gclass.TimeLeft,
						ExtraData = gclass.StoreObj
					});
				}
			}

			return health;
		}
	}
}
