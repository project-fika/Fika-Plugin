using EFT;
using LiteNetLib.Utils;
using static Fika.Core.Networking.SubPackets;
using static NetworkHealthSyncPacketStruct;
using static NetworkHealthSyncPacketStruct.NetworkHealthExtraDataTypeStruct;

namespace Fika.Core.Networking
{
    public struct HealthSyncPacket : INetSerializable
    {
        public int NetId;
        public NetworkHealthSyncPacketStruct Packet;
        public MongoID? KillerId;
        public MongoID? WeaponId;
        public EBodyPart BodyPart;
        public CorpseSyncPacket CorpseSyncPacket;
        public string[] TriggerZones;

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetInt();

            NetworkHealthSyncPacketStruct packet = new()
            {
                SyncType = (ESyncType)reader.GetByte()
            };
            ref NetworkHealthDataPacketStruct data = ref packet.Data;

            switch (packet.SyncType)
            {
                case ESyncType.AddEffect:
                    {
                        ref NetworkHealthExtraDataTypeStruct addEffect = ref data.AddEffect;
                        addEffect.EffectId = reader.GetInt();
                        addEffect.Type = reader.GetByte();
                        addEffect.BodyPart = (EBodyPart)reader.GetByte();
                        addEffect.DelayTime = reader.GetFloat();
                        addEffect.BuildUpTime = reader.GetFloat();
                        addEffect.WorkTime = reader.GetFloat();
                        addEffect.ResidueTime = reader.GetFloat();
                        addEffect.Strength = reader.GetPackedFloat(-100f, 100f, EFloatCompression.High);
                        addEffect.ExtraDataType = (EExtraDataType)reader.GetByte();

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
                    ref GStruct393 ens = ref data.EffectNextState;
                    ens.EffectId = reader.GetInt();
                    ens.StateTime = reader.GetFloat();
                    break;

                case ESyncType.EffectStateTime:
                    ref GStruct394 est = ref data.EffectStateTime;
                    est.EffectId = reader.GetInt();
                    est.RemainingStateTime = reader.GetFloat();
                    break;

                case ESyncType.EffectStrength:
                    ref GStruct395 estr = ref data.EffectStrength;
                    estr.EffectId = reader.GetInt();
                    estr.Strength = reader.GetPackedFloat(0f, 27f, EFloatCompression.High);
                    break;

                case ESyncType.EffectMedResource:
                    ref GStruct396 emr = ref data.EffectMedResource;
                    emr.EffectId = reader.GetInt();
                    emr.Resource = reader.GetPackedFloat(-1f, 3000f);
                    break;

                case ESyncType.EffectStimulatorBuff:
                    {
                        ref GStruct397 stim = ref data.EffectStimulatorBuff;
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
                        ref GStruct398 alive = ref data.IsAlive;
                        alive.IsAlive = reader.GetBool();

                        if (!alive.IsAlive)
                        {
                            alive.DamageType = (EDamageType)reader.GetInt();
                            KillerId = reader.GetMongoID();
                            WeaponId = reader.GetMongoID();
                            BodyPart = (EBodyPart)reader.GetByte();
                            CorpseSyncPacket = reader.GetCorpseSyncPacket();
                            TriggerZones = reader.GetStringArray();
                        }
                        break;
                    }

                case ESyncType.BodyHealth:
                    {
                        ref GStruct399 bh = ref data.BodyHealth;
                        bh.BodyPart = (EBodyPart)reader.GetByte();
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
                        ref GStruct403 dmg = ref data.ApplyDamage;
                        dmg.BodyPart = (EBodyPart)reader.GetByte();
                        dmg.Damage = reader.GetFloat();
                        dmg.DamageType = (EDamageType)reader.GetInt();
                        break;
                    }

                case ESyncType.DestroyedBodyPart:
                    {
                        ref GStruct404 destroyed = ref data.DestroyedBodyPart;
                        destroyed.BodyPart = (EBodyPart)reader.GetByte();
                        destroyed.IsDestroyed = reader.GetBool();

                        if (destroyed.IsDestroyed)
                        {
                            destroyed.DamageType = (EDamageType)reader.GetInt();
                        }
                        else
                        {
                            destroyed.HealthMaximum = reader.GetFloat();
                        }
                        break;
                    }

                case ESyncType.HealthRates:
                    {
                        ref GStruct405 rates = ref data.HealthRates;
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
                        ref GStruct407 burn = ref data.BurnEyes;
                        burn.Position = reader.GetVector3();
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

        public readonly void Serialize(NetDataWriter writer)
        {
            writer.Put(NetId);
            ref readonly NetworkHealthDataPacketStruct packet = ref Packet.Data;
            writer.Put((byte)Packet.SyncType);

            switch (Packet.SyncType)
            {
                case ESyncType.AddEffect:
                    {
                        ref readonly NetworkHealthExtraDataTypeStruct addEffect = ref packet.AddEffect;
                        writer.Put(addEffect.EffectId);
                        writer.Put(addEffect.Type);
                        writer.Put((byte)addEffect.BodyPart);
                        writer.Put(addEffect.DelayTime);
                        writer.Put(addEffect.BuildUpTime);
                        writer.Put(addEffect.WorkTime);
                        writer.Put(addEffect.ResidueTime);
                        writer.PutPackedFloat(addEffect.Strength, -100f, 100f, EFloatCompression.High);
                        writer.Put((byte)addEffect.ExtraDataType);

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
                        ref readonly GStruct393 ens = ref packet.EffectNextState;
                        writer.Put(ens.EffectId);
                        writer.Put(ens.StateTime);
                        break;
                    }

                case ESyncType.EffectStateTime:
                    {
                        ref readonly GStruct394 est = ref packet.EffectStateTime;
                        writer.Put(est.EffectId);
                        writer.Put(est.RemainingStateTime);
                        break;
                    }

                case ESyncType.EffectStrength:
                    {
                        ref readonly GStruct395 estr = ref packet.EffectStrength;
                        writer.Put(estr.EffectId);
                        writer.PutPackedFloat(estr.Strength, 0f, 27f, EFloatCompression.High);
                        break;
                    }

                case ESyncType.EffectMedResource:
                    {
                        ref readonly GStruct396 emr = ref packet.EffectMedResource;
                        writer.Put(emr.EffectId);
                        writer.PutPackedFloat(emr.Resource, -1f, 3000f);
                        break;
                    }

                case ESyncType.EffectStimulatorBuff:
                    {
                        ref readonly GStruct397 stim = ref packet.EffectStimulatorBuff;
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
                        ref readonly GStruct398 alive = ref packet.IsAlive;
                        writer.Put(alive.IsAlive);
                        if (!alive.IsAlive)
                        {
                            writer.Put((int)alive.DamageType);
                            writer.PutMongoID(KillerId);
                            writer.PutMongoID(WeaponId);
                            writer.Put((byte)BodyPart);
                            writer.PutCorpseSyncPacket(CorpseSyncPacket);
                            writer.PutArray(TriggerZones);
                        }
                        break;
                    }

                case ESyncType.BodyHealth:
                    {
                        ref readonly GStruct399 bh = ref packet.BodyHealth;
                        writer.Put((byte)bh.BodyPart);
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
                        ref readonly GStruct403 dmg = ref packet.ApplyDamage;
                        writer.Put((byte)dmg.BodyPart);
                        writer.Put(dmg.Damage);
                        writer.Put((int)dmg.DamageType);
                        break;
                    }

                case ESyncType.DestroyedBodyPart:
                    {
                        ref readonly GStruct404 destroyed = ref packet.DestroyedBodyPart;
                        writer.Put((byte)destroyed.BodyPart);
                        writer.Put(destroyed.IsDestroyed);
                        if (destroyed.IsDestroyed)
                            writer.Put((int)destroyed.DamageType);
                        else
                            writer.Put(destroyed.HealthMaximum);
                        break;
                    }

                case ESyncType.HealthRates:
                    {
                        ref readonly GStruct405 rates = ref packet.HealthRates;
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
                        ref readonly GStruct407 burn = ref packet.BurnEyes;
                        writer.PutVector3(burn.Position);
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

    }
}
