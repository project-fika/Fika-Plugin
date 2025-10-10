// © 2025 Lacyway All Rights Reserved

using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using System;
using System.Collections.Generic;

namespace Fika.Core.Main.ObservedClasses;

public sealed class ObservedHealthController(byte[] serializedState, ObservedPlayer player, InventoryController inventory, SkillManager skills)
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

    public override bool ApplyItem(Item item, GStruct382<EBodyPart> bodyPart, float? amount = null)
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
        for (int i = List_1.Count - 1; i >= 0; i--)
        {
            handler.PausedEffects.Add(List_1[i]);
            PausedEffectsStruct gstruct = Gclass835_0.Withdraw();
            gstruct.SaveInfo(List_1[i].Id, List_1[i].HealthController, List_1[i].Type, List_1[i].BodyPart, List_1[i].Strength,
                List_1[i].CurrentStrength, List_1[i].DelayTime, List_1[i].StateTime, List_1[i].WorkStateTime, List_1[i].BuildUpTime,
                List_1[i].ResidueTime, List_1[i].State);
            method_0(List_1[i]);
            List_1[i].ForceRemove();
            handler.PausedEffectsInfo.Add(gstruct);
        }

        Action_2 = new Action(handler.UnpauseEffects);
    }

    public override Profile.ProfileHealthClass Store(Profile.ProfileHealthClass health = null)
    {
        Profile.ProfileHealthClass profileHealthClass;
        if ((profileHealthClass = health) == null)
        {
            Profile.ProfileHealthClass profileHealthClass2 = new()
            {
                BodyParts = GClass866<EBodyPart>.GetDictWith<Profile.ProfileHealthClass.ProfileBodyPartHealthClass>(),
                Energy = new Profile.ProfileHealthClass.ValueInfo
                {
                    Current = HealthValue_0.Current,
                    Minimum = HealthValue_0.Minimum,
                    Maximum = HealthValue_0.Maximum
                },
                Hydration = new Profile.ProfileHealthClass.ValueInfo
                {
                    Current = HealthValue_1.Current,
                    Minimum = HealthValue_1.Minimum,
                    Maximum = HealthValue_1.Maximum
                },
                Temperature = new Profile.ProfileHealthClass.ValueInfo
                {
                    Current = HealthValue_2.Current,
                    Minimum = HealthValue_2.Minimum,
                    Maximum = HealthValue_2.Maximum
                }
            };
            profileHealthClass = profileHealthClass2;
            profileHealthClass2.Poison = new Profile.ProfileHealthClass.ValueInfo
            {
                Current = HealthValue_3.Current,
                Minimum = HealthValue_3.Minimum,
                Maximum = HealthValue_3.Maximum
            };
        }
        health = profileHealthClass;
        foreach (KeyValuePair<EBodyPart, BodyPartState> keyValuePair in Dictionary_0)
        {
            keyValuePair.Deconstruct(out EBodyPart ebodyPart, out BodyPartState bodyPartState);
            EBodyPart ebodyPart2 = ebodyPart;
            BodyPartState bodyPartState2 = bodyPartState;
            if (!health.BodyParts.TryGetValue(ebodyPart2, out Profile.ProfileHealthClass.ProfileBodyPartHealthClass gclass))
            {
                gclass = new Profile.ProfileHealthClass.ProfileBodyPartHealthClass();
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
            if (gclass is GInterface333 && gclass.State != EEffectState.Residued) // We only resync effects that are in-game effects, check for GClass increments, e.g. Dehydration or Exhaustion
            {
                Profile.ProfileHealthClass.ProfileBodyPartHealthClass gclass2 = health.BodyParts[gclass.BodyPart];
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
                healthController.Gclass835_0.Return(PausedEffectsInfo[i]);
            }
        }
    }
}
