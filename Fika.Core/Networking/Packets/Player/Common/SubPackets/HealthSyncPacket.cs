using System.Collections.Generic;
using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Pooling;
using static NetworkHealthSyncPacketStruct;
using static NetworkHealthSyncPacketStruct.NetworkHealthExtraDataTypeStruct;

namespace Fika.Core.Networking.Packets.Player.Common.SubPackets;

public sealed class HealthSyncPacket : IPoolSubPacket
{
    private HealthSyncPacket() { }

    public static HealthSyncPacket CreateInstance()
    {
        return new();
    }

    public NetworkHealthSyncPacketStruct Packet;
    public MongoID? KillerId;
    public MongoID? WeaponId;
    public EBodyPart BodyPart;
    public CorpseSyncPackets CorpseSyncPacket;
    public List<string> TriggerZones = new(4);

    public static HealthSyncPacket FromValue(NetworkHealthSyncPacketStruct value)
    {
        var packet = CommonSubPacketPoolManager.Instance.GetPacket<HealthSyncPacket>(ECommonSubPacketType.HealthSync);
        packet.Packet = value;
        return packet;
    }

    public void Execute(FikaPlayer player = null)
    {
        if (player is ObservedPlayer observedPlayer)
        {
            if (Packet.SyncType == ESyncType.IsAlive && !Packet.Data.IsAlive.IsAlive)
            {
                if (KillerId.HasValue)
                {
                    observedPlayer.SetAggressorData(KillerId, BodyPart, WeaponId);
                }
                observedPlayer.CorpseSyncPacket = CorpseSyncPacket;
                if (TriggerZones.Count > 0)
                {
                    observedPlayer.TriggerZones.AddRange(TriggerZones);
                }
            }
            observedPlayer.NetworkHealthController.HandleSyncPacket(Packet);
            return;
        }
        FikaGlobals.LogError($"OnHealthSyncPacketReceived::Player with id {player.NetId} was not observed. Name: {player.Profile.GetCorrectedNickname()}");
    }

    public void Deserialize(NetDataReader reader)
    {
        NetworkHealthSyncPacketStruct packet = new()
        {
            SyncType = reader.GetEnum<ESyncType>()
        };
        ref var data = ref packet.Data;

        switch (packet.SyncType)
        {
            case ESyncType.AddEffect:
                {
                    ref var addEffect = ref data.AddEffect;
                    addEffect.EffectId = reader.GetInt();
                    addEffect.Type = reader.GetByte();
                    addEffect.BodyPart = reader.GetEnum<EBodyPart>();
                    addEffect.DelayTime = reader.GetFloat();
                    addEffect.BuildUpTime = reader.GetFloat();
                    addEffect.WorkTime = reader.GetFloat();
                    addEffect.ResidueTime = reader.GetFloat();
                    addEffect.Strength = reader.GetPackedFloat(-100f, 100f, EFloatCompression.High);
                    addEffect.ExtraDataType = reader.GetEnum<EExtraDataType>();

                    switch (addEffect.ExtraDataType)
                    {
                        case EExtraDataType.MedEffect:
                            addEffect.ExtraData.MedEffect.ItemId = reader.GetMongoID();
                            addEffect.ExtraData.MedEffect.Amount = reader.GetFloat();
                            break;

                        case EExtraDataType.Stimulator:
                            addEffect.ExtraData.Stimulator.BuffsName = reader.GetString();
                            break;
                    }
                    break;
                }

            case ESyncType.RemoveEffect:
                data.RemoveEffect.EffectId = reader.GetInt();
                break;

            case ESyncType.EffectNextState:
                ref var ens = ref data.EffectNextState;
                ens.EffectId = reader.GetInt();
                ens.StateTime = reader.GetFloat();
                break;

            case ESyncType.EffectStateTime:
                ref var est = ref data.EffectStateTime;
                est.EffectId = reader.GetInt();
                est.RemainingStateTime = reader.GetFloat();
                break;

            case ESyncType.EffectStrength:
                ref var estr = ref data.EffectStrength;
                estr.EffectId = reader.GetInt();
                estr.Strength = reader.GetPackedFloat(0f, 27f, EFloatCompression.High);
                break;

            case ESyncType.EffectMedResource:
                ref var emr = ref data.EffectMedResource;
                emr.EffectId = reader.GetInt();
                emr.Resource = reader.GetPackedFloat(-1f, 3000f);
                break;

            case ESyncType.EffectStimulatorBuff:
                {
                    ref var stim = ref data.EffectStimulatorBuff;
                    stim.EffectId = reader.GetInt();
                    stim.BuffIndex = reader.GetPackedInt(0, 63);
                    stim.BuffActivate = reader.GetBool();

                    if (stim.BuffActivate)
                    {
                        stim.BuffValue = reader.GetFloat();
                        stim.BuffDuration = reader.GetFloat();
                        stim.BuffDelay = reader.GetFloat();
                    }
                    break;
                }

            case ESyncType.IsAlive:
                {
                    ref var alive = ref data.IsAlive;
                    alive.IsAlive = reader.GetBool();

                    if (!alive.IsAlive)
                    {
                        alive.DamageType = reader.GetEnum<EDamageType>();
                        KillerId = reader.GetNullableMongoID();
                        WeaponId = reader.GetNullableMongoID();
                        BodyPart = reader.GetEnum<EBodyPart>();
                        CorpseSyncPacket = reader.GetCorpseSyncPacket();
                        int count = reader.GetByte();
                        for (var i = 0; i < count; i++)
                        {
                            TriggerZones.Add(reader.GetString());
                        }
                    }
                    break;
                }

            case ESyncType.BodyHealth:
                {
                    ref var bh = ref data.BodyHealth;
                    bh.BodyPart = reader.GetEnum<EBodyPart>();
                    bh.Value = reader.GetFloat();
                    break;
                }

            case ESyncType.Energy:
                data.Energy.Value = reader.GetPackedFloat(0f, 200f, EFloatCompression.High);
                break;

            case ESyncType.Hydration:
                data.Hydration.Value = reader.GetPackedFloat(0f, 200f, EFloatCompression.High);
                break;

            case ESyncType.Temperature:
                data.Temperature.Value = reader.GetPackedFloat(0f, 100f, EFloatCompression.High);
                break;

            case ESyncType.DamageCoeff:
                data.DamageCoeff.DamageCoeff = reader.GetFloat();
                break;

            case ESyncType.ApplyDamage:
                {
                    ref var dmg = ref data.ApplyDamage;
                    dmg.BodyPart = reader.GetEnum<EBodyPart>();
                    dmg.Damage = reader.GetFloat();
                    dmg.DamageType = reader.GetEnum<EDamageType>();
                    break;
                }

            case ESyncType.DestroyedBodyPart:
                {
                    ref var destroyed = ref data.DestroyedBodyPart;
                    destroyed.BodyPart = reader.GetEnum<EBodyPart>();
                    destroyed.IsDestroyed = reader.GetBool();

                    if (destroyed.IsDestroyed)
                    {
                        destroyed.DamageType = reader.GetEnum<EDamageType>();
                    }
                    else
                    {
                        destroyed.HealthMaximum = reader.GetFloat();
                    }
                    break;
                }

            case ESyncType.HealthRates:
                {
                    ref var rates = ref data.HealthRates;
                    rates.HealRate = reader.GetPackedFloat(0f, 3000f, EFloatCompression.High);
                    rates.DamageRate = reader.GetPackedFloat(-1000f, 0f, EFloatCompression.High);
                    rates.DamageMultiplier = reader.GetPackedFloat(0f, 2f, EFloatCompression.High);
                    rates.Energy = reader.GetPackedFloat(-2000f, 3000f, EFloatCompression.High);
                    rates.Hydration = reader.GetPackedFloat(-2000f, 3000f, EFloatCompression.High);
                    rates.Temperature = reader.GetPackedFloat(-100f, 100f, EFloatCompression.High);
                    break;
                }

            case ESyncType.HealerDone:
                data.HealerDone.EffectId = reader.GetInt();
                break;

            case ESyncType.BurnEyes:
                {
                    ref var burn = ref data.BurnEyes;
                    burn.Position = reader.GetUnmanaged<Vector3>();
                    burn.DistanceStrength = reader.GetFloat();
                    burn.NormalTime = reader.GetFloat();
                    break;
                }

            case ESyncType.Poison:
                data.Poison.Value = reader.GetPackedFloat(0f, 100f, EFloatCompression.High);
                break;

            case ESyncType.StaminaCoeff:
                data.StaminaCoeff.StaminaCoeff = reader.GetFloat();
                break;
        }

        Packet = packet;
    }

    public void Serialize(NetDataWriter writer)
    {
        ref readonly var packet = ref Packet.Data;
        writer.PutEnum(Packet.SyncType);

        switch (Packet.SyncType)
        {
            case ESyncType.AddEffect:
                {
                    ref readonly var addEffect = ref packet.AddEffect;
                    writer.Put(addEffect.EffectId);
                    writer.Put(addEffect.Type);
                    writer.PutEnum(addEffect.BodyPart);
                    writer.Put(addEffect.DelayTime);
                    writer.Put(addEffect.BuildUpTime);
                    writer.Put(addEffect.WorkTime);
                    writer.Put(addEffect.ResidueTime);
                    writer.PutPackedFloat(addEffect.Strength, -100f, 100f, EFloatCompression.High);
                    writer.PutEnum(addEffect.ExtraDataType);

                    switch (addEffect.ExtraDataType)
                    {
                        case EExtraDataType.MedEffect:
                            writer.PutMongoID(addEffect.ExtraData.MedEffect.ItemId);
                            writer.Put(addEffect.ExtraData.MedEffect.Amount);
                            break;
                        case EExtraDataType.Stimulator:
                            writer.Put(addEffect.ExtraData.Stimulator.BuffsName);
                            break;
                    }
                    break;
                }

            case ESyncType.RemoveEffect:
                writer.Put(packet.RemoveEffect.EffectId);
                break;

            case ESyncType.EffectNextState:
                {
                    ref readonly var ens = ref packet.EffectNextState;
                    writer.Put(ens.EffectId);
                    writer.Put(ens.StateTime);
                    break;
                }

            case ESyncType.EffectStateTime:
                {
                    ref readonly var est = ref packet.EffectStateTime;
                    writer.Put(est.EffectId);
                    writer.Put(est.RemainingStateTime);
                    break;
                }

            case ESyncType.EffectStrength:
                {
                    ref readonly var estr = ref packet.EffectStrength;
                    writer.Put(estr.EffectId);
                    writer.PutPackedFloat(estr.Strength, 0f, 27f, EFloatCompression.High);
                    break;
                }

            case ESyncType.EffectMedResource:
                {
                    ref readonly var emr = ref packet.EffectMedResource;
                    writer.Put(emr.EffectId);
                    writer.PutPackedFloat(emr.Resource, -1f, 3000f);
                    break;
                }

            case ESyncType.EffectStimulatorBuff:
                {
                    ref readonly var stim = ref packet.EffectStimulatorBuff;
                    writer.Put(stim.EffectId);
                    writer.PutPackedInt(stim.BuffIndex, 0, 63);
                    writer.Put(stim.BuffActivate);
                    if (stim.BuffActivate)
                    {
                        writer.Put(stim.BuffValue);
                        writer.Put(stim.BuffDuration);
                        writer.Put(stim.BuffDelay);
                    }
                    break;
                }

            case ESyncType.IsAlive:
                {
                    ref readonly var alive = ref packet.IsAlive;
                    writer.Put(alive.IsAlive);
                    if (!alive.IsAlive)
                    {
                        writer.PutEnum(alive.DamageType);
                        writer.PutNullableMongoID(KillerId);
                        writer.PutNullableMongoID(WeaponId);
                        writer.PutEnum(BodyPart);
                        writer.PutCorpseSyncPacket(CorpseSyncPacket);
                        writer.Put((byte)TriggerZones.Count);
                        for (var i = 0; i < TriggerZones.Count; i++)
                        {
                            writer.Put(TriggerZones[i]);
                        }
                    }
                    break;
                }

            case ESyncType.BodyHealth:
                {
                    ref readonly var bh = ref packet.BodyHealth;
                    writer.PutEnum(bh.BodyPart);
                    writer.Put(bh.Value);
                    break;
                }

            case ESyncType.Energy:
                writer.PutPackedFloat(packet.Energy.Value, 0f, 200f, EFloatCompression.High);
                break;

            case ESyncType.Hydration:
                writer.PutPackedFloat(packet.Hydration.Value, 0f, 200f, EFloatCompression.High);
                break;

            case ESyncType.Temperature:
                writer.PutPackedFloat(packet.Temperature.Value, 0f, 100f, EFloatCompression.High);
                break;

            case ESyncType.DamageCoeff:
                writer.Put(packet.DamageCoeff.DamageCoeff);
                break;

            case ESyncType.ApplyDamage:
                {
                    ref readonly var dmg = ref packet.ApplyDamage;
                    writer.PutEnum(dmg.BodyPart);
                    writer.Put(dmg.Damage);
                    writer.PutEnum(dmg.DamageType);
                    break;
                }

            case ESyncType.DestroyedBodyPart:
                {
                    ref readonly var destroyed = ref packet.DestroyedBodyPart;
                    writer.PutEnum(destroyed.BodyPart);
                    writer.Put(destroyed.IsDestroyed);
                    if (destroyed.IsDestroyed)
                    {
                        writer.PutEnum(destroyed.DamageType);
                    }
                    else
                    {
                        writer.Put(destroyed.HealthMaximum);
                    }

                    break;
                }

            case ESyncType.HealthRates:
                {
                    ref readonly var rates = ref packet.HealthRates;
                    writer.PutPackedFloat(rates.HealRate, 0f, 3000f, EFloatCompression.High);
                    writer.PutPackedFloat(rates.DamageRate, -1000f, 0f, EFloatCompression.High);
                    writer.PutPackedFloat(rates.DamageMultiplier, 0f, 2f, EFloatCompression.High);
                    writer.PutPackedFloat(rates.Energy, -2000f, 3000f, EFloatCompression.High);
                    writer.PutPackedFloat(rates.Hydration, -2000f, 3000f, EFloatCompression.High);
                    writer.PutPackedFloat(rates.Temperature, -100f, 100f, EFloatCompression.High);
                    break;
                }

            case ESyncType.HealerDone:
                writer.Put(packet.HealerDone.EffectId);
                break;

            case ESyncType.BurnEyes:
                {
                    ref readonly var burn = ref packet.BurnEyes;
                    writer.PutUnmanaged(burn.Position);
                    writer.Put(burn.DistanceStrength);
                    writer.Put(burn.NormalTime);
                    break;
                }

            case ESyncType.Poison:
                writer.PutPackedFloat(packet.Poison.Value, 0f, 100f, EFloatCompression.High);
                break;

            case ESyncType.StaminaCoeff:
                writer.Put(packet.StaminaCoeff.StaminaCoeff);
                break;
        }
    }

    public void Dispose()
    {
        if (Packet.SyncType is ESyncType.IsAlive)
        {
            KillerId = null;
            WeaponId = null;
            BodyPart = default;
            CorpseSyncPacket = default;
            TriggerZones.Clear();
        }
        Packet = default;
    }
}

public struct CorpseSyncPackets
{
    public InventoryDescriptorClass InventoryDescriptor;
    public Item ItemInHands;

    public EBodyPartColliderType BodyPartColliderType;

    public Vector3 Direction;
    public Vector3 Point;

    public float Force;

    public EquipmentSlot ItemSlot;
}
