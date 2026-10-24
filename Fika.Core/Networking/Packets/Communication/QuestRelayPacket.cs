using System;
using System.Collections.Generic;
using EFT.HealthSystem;
using EFT.Quests;
using Fika.Core.Main.Utils;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace Fika.Core.Networking.Packets.Communication;

public struct QuestRelayPacket : INetSerializable
{
    public string Nickname;
    public EGroupQuestCounterEvent CounterEvent;
    public int ValueToAdd;
    public List<RelayedCheck> Checks;

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(Nickname);
        writer.PutEnum(CounterEvent);
        writer.Put(ValueToAdd);
        writer.Put((byte)Checks.Count);
        for (var i = 0; i < Checks.Count; i++)
        {
            QuestRelay.PutCheck(writer, Checks[i]);
        }
    }

    public void Deserialize(NetDataReader reader)
    {
        Nickname = reader.GetString();
        CounterEvent = reader.GetEnum<EGroupQuestCounterEvent>();
        ValueToAdd = reader.GetInt();
        var count = reader.GetByte();
        Checks = new List<RelayedCheck>(count);
        for (var i = 0; i < count; i++)
        {
            Checks.Add(QuestRelay.GetCheck(reader));
        }
    }
}

public enum ERelayedCheck : byte
{
    Kills,
    Shots,
    Location,
    HealthEffect,
    InZone,
    Time,
    HealthBuff,
    UseItem,
    LaunchFlare,
    VisitPlace,
    LocationTrigger,
    Equipment
}

public sealed class RelayedCheck
{
    public ERelayedCheck Type;
    public string Text;
    public int Count;
    public float Time;
    public List<string> Zones;
    public string[] Buffs;
    public RelayedEffects Effects;
    public RelayedHit Hit;
}

public sealed class RelayedEffects
{
    public float Hydration;
    public float Energy;
    public List<RelayedEffect> Effects;
}

public sealed class RelayedEffect
{
    public EBodyPart BodyPart;
    public string Name;
    public float Value;
}

public sealed class RelayedHit
{
    public string TargetSide;
    public float Distance;
    public string Weapon;
    public string WeaponCaliber;
    public string[] WeaponMods;
    public List<string> TargetEquipment;
    public EBodyPart BodyPart;
    public int CurrentHour;
    public int Value;
    public string ScavRole;
    public RelayedEffects EnemyEffects;
}

/// <summary>
/// Translates between the <see cref="ConditionCheck"/> arrays 1.1 hands to
/// <c>RelayPartySharedConditionProgress</c> and <see cref="QuestRelayPacket"/>.
/// </summary>
public static class QuestRelay
{
    /// <summary>
    /// The order each Check*Counter on ConditionalController builds its checks in
    /// </summary>
    private static readonly Dictionary<EGroupQuestCounterEvent, ERelayedCheck[]> _layouts = new()
    {
        [EGroupQuestCounterEvent.Kill] =
        [
            ERelayedCheck.Kills, ERelayedCheck.Location, ERelayedCheck.Equipment, ERelayedCheck.HealthEffect,
            ERelayedCheck.InZone, ERelayedCheck.Time, ERelayedCheck.HealthBuff
        ],
        [EGroupQuestCounterEvent.Shot] =
        [
            ERelayedCheck.Shots, ERelayedCheck.Location, ERelayedCheck.Equipment, ERelayedCheck.HealthEffect,
            ERelayedCheck.InZone, ERelayedCheck.Time, ERelayedCheck.HealthBuff
        ],
        [EGroupQuestCounterEvent.LaunchFlare] = [ERelayedCheck.LaunchFlare],
        [EGroupQuestCounterEvent.TriggerCondition] =
        [
            ERelayedCheck.VisitPlace, ERelayedCheck.Location, ERelayedCheck.Equipment, ERelayedCheck.HealthEffect,
            ERelayedCheck.LocationTrigger
        ],
        [EGroupQuestCounterEvent.UseItem] = [ERelayedCheck.UseItem, ERelayedCheck.Location, ERelayedCheck.InZone]
    };

    private static readonly HashSet<EGroupQuestCounterEvent> _warned = [];

    public static bool TryBuild(string nickname, EGroupQuestCounterEvent counterEvent, int valueToAdd,
        Il2CppReferenceArray<ConditionCheck> checks, string target, out QuestRelayPacket packet)
    {
        packet = default;
        if (!_layouts.TryGetValue(counterEvent, out var layout))
        {
            return false;
        }

        if (checks == null || checks.Length != layout.Length)
        {
            if (_warned.Add(counterEvent))
            {
                FikaGlobals.LogWarning($"QuestRelay: {counterEvent} had {checks?.Length ?? 0} checks, expected {layout.Length}, not relaying");
            }
            return false;
        }

        List<RelayedCheck> relayed = new(layout.Length);
        for (var i = 0; i < layout.Length; i++)
        {
            if (layout[i] is ERelayedCheck.Equipment)
            {
                continue;
            }

            var check = Flatten(layout[i], checks[i].testValue, target);
            if (check == null)
            {
                return false;
            }
            relayed.Add(check);
        }

        packet = new QuestRelayPacket
        {
            Nickname = nickname,
            CounterEvent = counterEvent,
            ValueToAdd = valueToAdd,
            Checks = relayed
        };
        return true;
    }

    public static Il2CppReferenceArray<ConditionCheck> ToIl2Cpp(List<RelayedCheck> checks)
    {
        var result = new Il2CppReferenceArray<ConditionCheck>((long)checks.Count);
        for (var i = 0; i < checks.Count; i++)
        {
            result[i] = Rebuild(checks[i]);
        }
        return result;
    }

    private static RelayedCheck Flatten(ERelayedCheck type, Il2CppSystem.Object value, string target)
    {
        switch (type)
        {
            case ERelayedCheck.Kills:
            case ERelayedCheck.Shots:
                {
                    var hit = value?.TryCast<HitConditionCheck>();
                    return hit == null ? null : new RelayedCheck { Type = type, Hit = FlattenHit(hit) };
                }
            case ERelayedCheck.Location:
            case ERelayedCheck.UseItem:
            case ERelayedCheck.LaunchFlare:
                return new RelayedCheck { Type = type, Text = ReadText(value) };
            case ERelayedCheck.HealthEffect:
                return new RelayedCheck { Type = type, Effects = FlattenEffects(value?.TryCast<HealthEffects>()) };
            case ERelayedCheck.InZone:
                return new RelayedCheck { Type = type, Zones = ReadTextList(value) };
            case ERelayedCheck.Time:
                return new RelayedCheck { Type = type, Time = value != null ? value.Unbox<float>() : 0f };
            case ERelayedCheck.HealthBuff:
                return new RelayedCheck { Type = type, Buffs = ReadTextArray(value) };
            case ERelayedCheck.VisitPlace:
            case ERelayedCheck.LocationTrigger:
                return new RelayedCheck { Type = type, Text = target, Count = value != null ? value.Unbox<int>() : 0 };
        }

        return null;
    }

    private static ConditionCheck Rebuild(RelayedCheck check)
    {
        return check.Type switch
        {
            ERelayedCheck.Kills => Identity<ConditionKills>().Test(RebuildHit(check.Hit)),
            ERelayedCheck.Shots => Identity<ConditionShots>().Test(RebuildHit(check.Hit)),
            ERelayedCheck.Location => Identity<ConditionLocation>().Test(Text(check.Text)),
            ERelayedCheck.HealthEffect => Identity<ConditionHealthEffect>().Test(RebuildEffects(check.Effects)),
            ERelayedCheck.InZone => Identity<ConditionInZone>().Test(check.Zones?.ToIl2CppList()),
            ERelayedCheck.Time => Identity<ConditionTime>().Test(Box(check.Time)),
            ERelayedCheck.HealthBuff => Identity<ConditionHealthBuff>().Test(AsObject(RebuildTextArray(check.Buffs))),
            ERelayedCheck.UseItem => Identity<ConditionUseItem>().Test(Text(check.Text)),
            ERelayedCheck.LaunchFlare => Identity<ConditionLaunchFlare>().Test(Text(check.Text)),
            ERelayedCheck.VisitPlace => Identity<ConditionVisitPlace>(check.Text).Test(Box(check.Count)),
            ERelayedCheck.LocationTrigger => Identity<ConditionLocationTrigger>(check.Text).Test(Box(check.Count)),
            _ => throw new ArgumentOutOfRangeException(nameof(check))
        };
    }

    private static ConditionCheck Identity<T>()
    {
        return new ConditionCheck(new Il2CppReferenceArray<Il2CppSystem.Object>(new Il2CppSystem.Object[] { Il2CppType.Of<T>() }));
    }

    private static ConditionCheck Identity<T>(string target)
    {
        return new ConditionCheck(new Il2CppReferenceArray<Il2CppSystem.Object>(
            new Il2CppSystem.Object[] { Il2CppType.Of<T>(), Text(target) }));
    }

    private static RelayedHit FlattenHit(HitConditionCheck hit)
    {
        return new RelayedHit
        {
            TargetSide = hit.TargetSide,
            Distance = hit.Distance,
            Weapon = hit.Weapon,
            WeaponCaliber = hit.WeaponCaliber,
            WeaponMods = ReadTextArray(hit.WeaponMods),
            TargetEquipment = hit.TargetEquipment?.ToManagedList(),
            BodyPart = hit.BodyPart,
            CurrentHour = hit.CurrentHour,
            Value = hit.Value,
            ScavRole = hit.ScavRole,
            EnemyEffects = FlattenEffects(hit.EnemyHealthEffects)
        };
    }

    private static HitConditionCheck RebuildHit(RelayedHit hit)
    {
        return new HitConditionCheck
        {
            TargetSide = hit.TargetSide,
            Distance = hit.Distance,
            Weapon = hit.Weapon,
            WeaponCaliber = hit.WeaponCaliber,
            WeaponMods = RebuildTextArray(hit.WeaponMods),
            TargetEquipment = hit.TargetEquipment?.ToIl2CppList(),
            BodyPart = hit.BodyPart,
            CurrentHour = hit.CurrentHour,
            Value = hit.Value,
            ScavRole = hit.ScavRole,
            EnemyHealthEffects = RebuildEffects(hit.EnemyEffects)
        };
    }

    private static RelayedEffects FlattenEffects(HealthEffects effects)
    {
        if (effects == null)
        {
            return null;
        }

        List<RelayedEffect> flat = [];
        if (effects.Effects != null)
        {
            foreach (var bodyPart in effects.Effects)
            {
                if (bodyPart.Value == null)
                {
                    continue;
                }

                foreach (var effect in bodyPart.Value)
                {
                    flat.Add(new RelayedEffect
                    {
                        BodyPart = bodyPart.Key,
                        Name = effect.Key,
                        Value = effect.Value
                    });
                }
            }
        }

        return new RelayedEffects
        {
            Hydration = effects.Hydration,
            Energy = effects.Energy,
            Effects = flat
        };
    }

    private static HealthEffects RebuildEffects(RelayedEffects effects)
    {
        if (effects == null)
        {
            return null;
        }

        Il2CppSystem.Collections.Generic.Dictionary<EBodyPart, Il2CppSystem.Collections.Generic.Dictionary<string, float>> byBodyPart = new();
        for (var i = 0; i < effects.Effects.Count; i++)
        {
            var effect = effects.Effects[i];
            if (!byBodyPart.TryGetValue(effect.BodyPart, out var bucket))
            {
                bucket = new Il2CppSystem.Collections.Generic.Dictionary<string, float>();
                byBodyPart[effect.BodyPart] = bucket;
            }
            bucket[effect.Name] = effect.Value;
        }

        return new HealthEffects
        {
            Hydration = effects.Hydration,
            Energy = effects.Energy,
            Effects = byBodyPart
        };
    }

    private static Il2CppSystem.String Text(string value)
    {
        return value;
    }

    private static Il2CppSystem.Object AsObject(Il2CppObjectBase value)
    {
        return value != null ? new Il2CppSystem.Object(value.Pointer) : null;
    }

    private static string ReadText(Il2CppSystem.Object value)
    {
        return value != null ? IL2CPP.Il2CppStringToManaged(value.Pointer) : null;
    }

    private static List<string> ReadTextList(Il2CppSystem.Object value)
    {
        return value != null ? new Il2CppSystem.Collections.Generic.List<string>(value.Pointer).ToManagedList() : null;
    }

    private static string[] ReadTextArray(Il2CppSystem.Object value)
    {
        return value != null ? ReadTextArray(new Il2CppStringArray(value.Pointer)) : null;
    }

    private static string[] ReadTextArray(Il2CppStringArray value)
    {
        if (value == null)
        {
            return null;
        }

        var result = new string[value.Length];
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = value[i];
        }
        return result;
    }

    private static Il2CppStringArray RebuildTextArray(string[] value)
    {
        return value != null ? new Il2CppStringArray(value) : null;
    }

    private static unsafe Il2CppSystem.Object Box<T>(T value) where T : unmanaged
    {
        return new Il2CppSystem.Object(IL2CPP.il2cpp_value_box(Il2CppClassPointerStore<T>.NativeClassPtr, (IntPtr)(&value)));
    }

    internal static void PutCheck(NetDataWriter writer, RelayedCheck check)
    {
        writer.PutEnum(check.Type);
        switch (check.Type)
        {
            case ERelayedCheck.Kills:
            case ERelayedCheck.Shots:
                PutHit(writer, check.Hit);
                break;
            case ERelayedCheck.Location:
            case ERelayedCheck.UseItem:
            case ERelayedCheck.LaunchFlare:
                writer.Put(check.Text);
                break;
            case ERelayedCheck.HealthEffect:
                PutEffects(writer, check.Effects);
                break;
            case ERelayedCheck.InZone:
                PutTextList(writer, check.Zones);
                break;
            case ERelayedCheck.Time:
                writer.Put(check.Time);
                break;
            case ERelayedCheck.HealthBuff:
                PutTextArray(writer, check.Buffs);
                break;
            case ERelayedCheck.VisitPlace:
            case ERelayedCheck.LocationTrigger:
                writer.Put(check.Text);
                writer.Put(check.Count);
                break;
        }
    }

    internal static RelayedCheck GetCheck(NetDataReader reader)
    {
        RelayedCheck check = new()
        {
            Type = reader.GetEnum<ERelayedCheck>()
        };

        switch (check.Type)
        {
            case ERelayedCheck.Kills:
            case ERelayedCheck.Shots:
                check.Hit = GetHit(reader);
                break;
            case ERelayedCheck.Location:
            case ERelayedCheck.UseItem:
            case ERelayedCheck.LaunchFlare:
                check.Text = reader.GetString();
                break;
            case ERelayedCheck.HealthEffect:
                check.Effects = GetEffects(reader);
                break;
            case ERelayedCheck.InZone:
                check.Zones = GetTextList(reader);
                break;
            case ERelayedCheck.Time:
                check.Time = reader.GetFloat();
                break;
            case ERelayedCheck.HealthBuff:
                check.Buffs = GetTextArray(reader);
                break;
            case ERelayedCheck.VisitPlace:
            case ERelayedCheck.LocationTrigger:
                check.Text = reader.GetString();
                check.Count = reader.GetInt();
                break;
        }

        return check;
    }

    private static void PutHit(NetDataWriter writer, RelayedHit hit)
    {
        writer.Put(hit.TargetSide);
        writer.Put(hit.Distance);
        writer.Put(hit.Weapon);
        writer.Put(hit.WeaponCaliber);
        PutTextArray(writer, hit.WeaponMods);
        PutTextList(writer, hit.TargetEquipment);
        writer.PutEnum(hit.BodyPart);
        writer.Put(hit.CurrentHour);
        writer.Put(hit.Value);
        writer.Put(hit.ScavRole);
        PutEffects(writer, hit.EnemyEffects);
    }

    private static RelayedHit GetHit(NetDataReader reader)
    {
        return new RelayedHit
        {
            TargetSide = reader.GetString(),
            Distance = reader.GetFloat(),
            Weapon = reader.GetString(),
            WeaponCaliber = reader.GetString(),
            WeaponMods = GetTextArray(reader),
            TargetEquipment = GetTextList(reader),
            BodyPart = reader.GetEnum<EBodyPart>(),
            CurrentHour = reader.GetInt(),
            Value = reader.GetInt(),
            ScavRole = reader.GetString(),
            EnemyEffects = GetEffects(reader)
        };
    }

    private static void PutEffects(NetDataWriter writer, RelayedEffects effects)
    {
        writer.Put(effects != null);
        if (effects == null)
        {
            return;
        }

        writer.Put(effects.Hydration);
        writer.Put(effects.Energy);
        writer.Put((ushort)effects.Effects.Count);
        for (var i = 0; i < effects.Effects.Count; i++)
        {
            var effect = effects.Effects[i];
            writer.PutEnum(effect.BodyPart);
            writer.Put(effect.Name);
            writer.Put(effect.Value);
        }
    }

    private static RelayedEffects GetEffects(NetDataReader reader)
    {
        if (!reader.GetBool())
        {
            return null;
        }

        RelayedEffects effects = new()
        {
            Hydration = reader.GetFloat(),
            Energy = reader.GetFloat()
        };

        var count = reader.GetUShort();
        effects.Effects = new List<RelayedEffect>(count);
        for (var i = 0; i < count; i++)
        {
            effects.Effects.Add(new RelayedEffect
            {
                BodyPart = reader.GetEnum<EBodyPart>(),
                Name = reader.GetString(),
                Value = reader.GetFloat()
            });
        }

        return effects;
    }

    private static void PutTextList(NetDataWriter writer, List<string> values)
    {
        writer.Put(values != null);
        if (values != null)
        {
            writer.PutArray(values.ToArray());
        }
    }

    private static List<string> GetTextList(NetDataReader reader)
    {
        if (!reader.GetBool())
        {
            return null;
        }

        return [.. reader.GetStringArray()];
    }

    private static void PutTextArray(NetDataWriter writer, string[] values)
    {
        writer.Put(values != null);
        if (values != null)
        {
            writer.PutArray(values);
        }
    }

    private static string[] GetTextArray(NetDataReader reader)
    {
        return reader.GetBool() ? reader.GetStringArray() : null;
    }
}
