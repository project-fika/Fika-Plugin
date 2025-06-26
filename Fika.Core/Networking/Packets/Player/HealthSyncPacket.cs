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
        public string KillerId;
        public string WeaponId;
        public EBodyPart BodyPart;
        public CorpseSyncPacket CorpseSyncPacket;
        public string[] TriggerZones;

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetInt();

            NetworkHealthSyncPacketStruct packet = new();
            packet.SyncType = (NetworkHealthSyncPacketStruct.ESyncType)reader.GetByte();
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
                        addEffect.Strength = reader.GetFloat();
                        addEffect.ExtraDataType = (NetworkHealthSyncPacketStruct.NetworkHealthExtraDataTypeStruct.EExtraDataType)reader.GetByte();

                        switch (addEffect.ExtraDataType)
                        {
                            case EExtraDataType.MedEffect:
                                addEffect.ExtraData.MedEffect.ItemId = reader.GetString();
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
                    estr.Strength = reader.GetFloat();
                    break;

                case ESyncType.EffectMedResource:
                    ref GStruct396 emr = ref data.EffectMedResource;
                    emr.EffectId = reader.GetInt();
                    emr.Resource = reader.GetFloat();
                    break;

                case ESyncType.EffectStimulatorBuff:
                    {
                        ref GStruct397 stim = ref data.EffectStimulatorBuff;
                        stim.EffectId = reader.GetInt();
                        stim.BuffIndex = reader.GetInt();
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
                            KillerId = reader.GetString();
                            WeaponId = reader.GetString();
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
                    data.Energy.Value = reader.GetFloat();
                    break;

                case ESyncType.Hydration:
                    data.Hydration.Value = reader.GetFloat();
                    break;

                case ESyncType.Temperature:
                    data.Temperature.Value = reader.GetFloat();
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
                        rates.HealRate = reader.GetFloat();
                        rates.DamageRate = reader.GetFloat();
                        rates.DamageMultiplier = reader.GetFloat();
                        rates.Energy = reader.GetFloat();
                        rates.Hydration = reader.GetFloat();
                        rates.Temperature = reader.GetFloat();
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
                    data.Poison.Value = reader.GetFloat();
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
            NetworkHealthDataPacketStruct packet = Packet.Data;
            writer.Put((byte)Packet.SyncType);
            switch (Packet.SyncType)
            {
                case ESyncType.AddEffect:
                    {
                        writer.Put(packet.AddEffect.EffectId);
                        writer.Put(packet.AddEffect.Type);
                        writer.Put((byte)packet.AddEffect.BodyPart);
                        writer.Put(packet.AddEffect.DelayTime);
                        writer.Put(packet.AddEffect.BuildUpTime);
                        writer.Put(packet.AddEffect.WorkTime);
                        writer.Put(packet.AddEffect.ResidueTime);
                        writer.Put(packet.AddEffect.Strength);
                        writer.Put((byte)packet.AddEffect.ExtraDataType);
                        switch (packet.AddEffect.ExtraDataType)
                        {
                            case EExtraDataType.None:
                                break;
                            case EExtraDataType.MedEffect:
                                {
                                    writer.Put(packet.AddEffect.ExtraData.MedEffect.ItemId);
                                    writer.Put(packet.AddEffect.ExtraData.MedEffect.Amount);
                                    break;
                                }
                            case EExtraDataType.Stimulator:
                                {
                                    writer.Put(packet.AddEffect.ExtraData.Stimulator.BuffsName);
                                    break;
                                }
                        }
                        break;
                    }
                case ESyncType.RemoveEffect:
                    {
                        writer.Put(packet.RemoveEffect.EffectId);
                        break;
                    }
                case ESyncType.EffectNextState:
                    {
                        writer.Put(packet.EffectNextState.EffectId);
                        writer.Put(packet.EffectNextState.StateTime);
                        break;
                    }
                case ESyncType.EffectStateTime:
                    {
                        writer.Put(packet.EffectStateTime.EffectId);
                        writer.Put(packet.EffectStateTime.RemainingStateTime);
                        break;
                    }
                case ESyncType.EffectStrength:
                    {
                        writer.Put(packet.EffectStrength.EffectId);
                        writer.Put(packet.EffectStrength.Strength);
                        break;
                    }
                case ESyncType.EffectMedResource:
                    {
                        writer.Put(packet.EffectMedResource.EffectId);
                        writer.Put(packet.EffectMedResource.Resource);
                        break;
                    }
                case ESyncType.EffectStimulatorBuff:
                    {
                        writer.Put(packet.EffectStimulatorBuff.EffectId);
                        writer.Put(packet.EffectStimulatorBuff.BuffIndex);
                        writer.Put(packet.EffectStimulatorBuff.BuffActivate);
                        if (packet.EffectStimulatorBuff.BuffActivate)
                        {
                            writer.Put(packet.EffectStimulatorBuff.BuffValue);
                            writer.Put(packet.EffectStimulatorBuff.BuffDuration);
                            writer.Put(packet.EffectStimulatorBuff.BuffDelay);
                            break;
                        }
                        break;
                    }
                case ESyncType.IsAlive:
                    {
                        writer.Put(packet.IsAlive.IsAlive);
                        if (!packet.IsAlive.IsAlive)
                        {
                            writer.Put((int)packet.IsAlive.DamageType);
                            writer.Put(KillerId);
                            writer.Put(WeaponId);
                            writer.Put((byte)BodyPart);
                            writer.PutCorpseSyncPacket(CorpseSyncPacket);
                            writer.PutArray(TriggerZones);
                            break;
                        }
                        break;
                    }
                case ESyncType.BodyHealth:
                    {
                        writer.Put((byte)packet.BodyHealth.BodyPart);
                        writer.Put(packet.BodyHealth.Value);
                        break;
                    }
                case ESyncType.Energy:
                    {
                        writer.Put(packet.Energy.Value);
                        break;
                    }
                case ESyncType.Hydration:
                    {
                        writer.Put(packet.Hydration.Value);
                        break;
                    }
                case ESyncType.Temperature:
                    {
                        writer.Put(packet.Temperature.Value);
                        break;
                    }
                case ESyncType.DamageCoeff:
                    {
                        writer.Put(packet.DamageCoeff.DamageCoeff);
                        break;
                    }
                case ESyncType.ApplyDamage:
                    {
                        writer.Put((byte)packet.ApplyDamage.BodyPart);
                        writer.Put(packet.ApplyDamage.Damage);
                        writer.Put((int)packet.ApplyDamage.DamageType);
                        break;
                    }
                case ESyncType.DestroyedBodyPart:
                    {
                        writer.Put((byte)packet.DestroyedBodyPart.BodyPart);
                        writer.Put(packet.DestroyedBodyPart.IsDestroyed);
                        if (packet.DestroyedBodyPart.IsDestroyed)
                        {
                            writer.Put((int)packet.DestroyedBodyPart.DamageType);
                            break;
                        }
                        writer.Put(packet.DestroyedBodyPart.HealthMaximum);
                        break;
                    }
                case ESyncType.HealthRates:
                    {
                        writer.Put(packet.HealthRates.HealRate);
                        writer.Put(packet.HealthRates.DamageRate);
                        writer.Put(packet.HealthRates.DamageMultiplier);
                        writer.Put(packet.HealthRates.Energy);
                        writer.Put(packet.HealthRates.Hydration);
                        writer.Put(packet.HealthRates.Temperature);
                        break;
                    }
                case ESyncType.HealerDone:
                    {
                        writer.Put(packet.HealerDone.EffectId);
                        break;
                    }
                case ESyncType.BurnEyes:
                    {
                        writer.PutVector3(packet.BurnEyes.Position);
                        writer.Put(packet.BurnEyes.DistanceStrength);
                        writer.Put(packet.BurnEyes.NormalTime);
                        break;
                    }
                case ESyncType.Poison:
                    {
                        writer.Put(packet.Poison.Value);
                        break;
                    }
                case ESyncType.StaminaCoeff:
                    {
                        writer.Put(packet.StaminaCoeff.StaminaCoeff);
                        break;
                    }
            }
        }
    }
}
