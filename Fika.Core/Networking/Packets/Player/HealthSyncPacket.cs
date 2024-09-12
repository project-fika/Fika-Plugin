using EFT;
using LiteNetLib.Utils;
using static Fika.Core.Networking.FikaSerialization;

namespace Fika.Core.Networking
{
	public struct HealthSyncPacket(int netId) : INetSerializable
	{
		public int NetId = netId;
		public GStruct352 Packet;
		public string KillerId;
		public RagdollPacket RagdollPacket;
		public string[] TriggerZones;

		public void Deserialize(NetDataReader reader)
		{
			NetId = reader.GetInt();
			GStruct352 packet = new()
			{
				SyncType = (GStruct352.ESyncType)reader.GetInt()
			};
			switch (packet.SyncType)
			{
				case GStruct352.ESyncType.AddEffect:
					{
						packet.Data.AddEffect = new()
						{
							EffectId = reader.GetInt(),
							Type = reader.GetByte(),
							BodyPart = (EBodyPart)reader.GetInt(),
							DelayTime = reader.GetFloat(),
							BuildUpTime = reader.GetFloat(),
							WorkTime = reader.GetFloat(),
							ResidueTime = reader.GetFloat(),
							Strength = reader.GetFloat(),
							ExtraDataType = (GStruct352.GStruct353.EExtraDataType)reader.GetInt()
						};
						switch (packet.Data.AddEffect.ExtraDataType)
						{
							case GStruct352.GStruct353.EExtraDataType.MedEffect:
								{
									packet.Data.AddEffect.ExtraData = new()
									{
										MedEffect = new()
										{
											ItemId = reader.GetString(),
											Amount = reader.GetFloat()
										}
									};
								}
								break;
							case GStruct352.GStruct353.EExtraDataType.Stimulator:
								{
									packet.Data.AddEffect.ExtraData = new()
									{
										Stimulator = new()
										{
											BuffsName = reader.GetString()
										}
									};
									break;
								}
						}
						break;
					}
				case GStruct352.ESyncType.RemoveEffect:
					{
						packet.Data.RemoveEffect = new()
						{
							EffectId = reader.GetInt()
						};
						break;
					}
				case GStruct352.ESyncType.EffectNextState:
					{
						packet.Data.EffectNextState = new()
						{
							EffectId = reader.GetInt(),
							StateTime = reader.GetFloat()
						};
						break;
					}
				case GStruct352.ESyncType.EffectStateTime:
					{
						packet.Data.EffectStateTime = new()
						{
							EffectId = reader.GetInt(),
							RemainingStateTime = reader.GetFloat()
						};
						break;
					}
				case GStruct352.ESyncType.EffectStrength:
					{
						packet.Data.EffectStrength = new()
						{
							EffectId = reader.GetInt(),
							Strength = reader.GetFloat()
						};
						break;
					}
				case GStruct352.ESyncType.EffectMedResource:
					{
						packet.Data.EffectMedResource = new()
						{
							EffectId = reader.GetInt(),
							Resource = reader.GetFloat()
						};
						break;
					}
				case GStruct352.ESyncType.EffectStimulatorBuff:
					{
						packet.Data.EffectStimulatorBuff = new()
						{
							EffectId = reader.GetInt(),
							BuffIndex = reader.GetInt(),
							BuffActivate = reader.GetBool()
						};
						if (packet.Data.EffectStimulatorBuff.BuffActivate)
						{
							packet.Data.EffectStimulatorBuff.BuffValue = reader.GetFloat();
							packet.Data.EffectStimulatorBuff.BuffDuration = reader.GetFloat();
							packet.Data.EffectStimulatorBuff.BuffDelay = reader.GetFloat();
							break;
						}
						break;
					}
				case GStruct352.ESyncType.IsAlive:
					{
						packet.Data.IsAlive = new()
						{
							IsAlive = reader.GetBool()
						};
						if (!packet.Data.IsAlive.IsAlive)
						{
							packet.Data.IsAlive.DamageType = (EDamageType)reader.GetInt();
							KillerId = reader.GetString();
							RagdollPacket = RagdollPacket.Deserialize(reader);
							TriggerZones = reader.GetStringArray();
							break;
						}
						break;
					}
				case GStruct352.ESyncType.BodyHealth:
					{
						packet.Data.BodyHealth = new()
						{
							BodyPart = (EBodyPart)reader.GetInt(),
							Value = reader.GetFloat()
						};
						break;
					}
				case GStruct352.ESyncType.Energy:
					{
						packet.Data.Energy = new()
						{
							Value = reader.GetFloat()
						};
						break;
					}
				case GStruct352.ESyncType.Hydration:
					{
						packet.Data.Hydration = new()
						{
							Value = reader.GetFloat()
						};
						break;
					}
				case GStruct352.ESyncType.Temperature:
					{
						packet.Data.Temperature = new()
						{
							Value = reader.GetFloat()
						};
						break;
					}
				case GStruct352.ESyncType.DamageCoeff:
					{
						packet.Data.DamageCoeff = new()
						{
							DamageCoeff = reader.GetFloat()
						};
						break;
					}
				case GStruct352.ESyncType.ApplyDamage:
					{
						packet.Data.ApplyDamage = new()
						{
							BodyPart = (EBodyPart)reader.GetInt(),
							Damage = reader.GetFloat(),
							DamageType = (EDamageType)reader.GetInt()
						};
						break;
					}
				case GStruct352.ESyncType.DestroyedBodyPart:
					{
						packet.Data.DestroyedBodyPart = new()
						{
							BodyPart = (EBodyPart)reader.GetInt(),
							IsDestroyed = reader.GetBool()
						};
						if (packet.Data.DestroyedBodyPart.IsDestroyed)
						{
							packet.Data.DestroyedBodyPart.DamageType = (EDamageType)reader.GetInt();
							break;
						}
						packet.Data.DestroyedBodyPart.HealthMaximum = reader.GetFloat();
						break;
					}
				case GStruct352.ESyncType.HealthRates:
					{
						packet.Data.HealthRates = new()
						{
							HealRate = reader.GetFloat(),
							DamageRate = reader.GetFloat(),
							DamageMultiplier = reader.GetFloat(),
							Energy = reader.GetFloat(),
							Hydration = reader.GetFloat(),
							Temperature = reader.GetFloat()
						};
						break;
					}
				case GStruct352.ESyncType.HealerDone:
					{
						packet.Data.HealerDone = new()
						{
							EffectId = reader.GetInt()
						};
						break;
					}
				case GStruct352.ESyncType.BurnEyes:
					{
						packet.Data.BurnEyes = new()
						{
							Position = reader.GetVector3(),
							DistanceStrength = reader.GetFloat(),
							NormalTime = reader.GetFloat()
						};
						break;
					}
				case GStruct352.ESyncType.Poison:
					{
						packet.Data.Poison = new()
						{
							Value = reader.GetFloat()
						};
						break;
					}
				case GStruct352.ESyncType.StaminaCoeff:
					{
						packet.Data.StaminaCoeff = new()
						{
							StaminaCoeff = reader.GetFloat()
						};
						return;
					}
			}

			Packet = packet;
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(NetId);
			GStruct352.GStruct371 packet = Packet.Data;
			writer.Put((int)Packet.SyncType);
			switch (Packet.SyncType)
			{
				case GStruct352.ESyncType.AddEffect:
					{
						writer.Put(packet.AddEffect.EffectId);
						writer.Put(packet.AddEffect.Type);
						writer.Put((int)packet.AddEffect.BodyPart);
						writer.Put(packet.AddEffect.DelayTime);
						writer.Put(packet.AddEffect.BuildUpTime);
						writer.Put(packet.AddEffect.WorkTime);
						writer.Put(packet.AddEffect.ResidueTime);
						writer.Put(packet.AddEffect.Strength);
						writer.Put((int)packet.AddEffect.ExtraDataType);
						switch (packet.AddEffect.ExtraDataType)
						{
							case GStruct352.GStruct353.EExtraDataType.None:
								break;
							case GStruct352.GStruct353.EExtraDataType.MedEffect:
								{
									writer.Put(packet.AddEffect.ExtraData.MedEffect.ItemId);
									writer.Put(packet.AddEffect.ExtraData.MedEffect.Amount);
									break;
								}
							case GStruct352.GStruct353.EExtraDataType.Stimulator:
								{
									writer.Put(packet.AddEffect.ExtraData.Stimulator.BuffsName);
									break;
								}
						}
						break;
					}
				case GStruct352.ESyncType.RemoveEffect:
					{
						writer.Put(packet.RemoveEffect.EffectId);
						break;
					}
				case GStruct352.ESyncType.EffectNextState:
					{
						writer.Put(packet.EffectNextState.EffectId);
						writer.Put(packet.EffectNextState.StateTime);
						break;
					}
				case GStruct352.ESyncType.EffectStateTime:
					{
						writer.Put(packet.EffectStateTime.EffectId);
						writer.Put(packet.EffectStateTime.RemainingStateTime);
						break;
					}
				case GStruct352.ESyncType.EffectStrength:
					{
						writer.Put(packet.EffectStrength.EffectId);
						writer.Put(packet.EffectStrength.Strength);
						break;
					}
				case GStruct352.ESyncType.EffectMedResource:
					{
						writer.Put(packet.EffectMedResource.EffectId);
						writer.Put(packet.EffectMedResource.Resource);
						break;
					}
				case GStruct352.ESyncType.EffectStimulatorBuff:
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
				case GStruct352.ESyncType.IsAlive:
					{
						writer.Put(packet.IsAlive.IsAlive);
						if (!packet.IsAlive.IsAlive)
						{
							writer.Put((int)packet.IsAlive.DamageType);
							writer.Put(KillerId);
							RagdollPacket.Serialize(writer, RagdollPacket);
							writer.PutArray(TriggerZones);
							break;
						}
						break;
					}
				case GStruct352.ESyncType.BodyHealth:
					{
						writer.Put((int)packet.BodyHealth.BodyPart);
						writer.Put(packet.BodyHealth.Value);
						break;
					}
				case GStruct352.ESyncType.Energy:
					{
						writer.Put(packet.Energy.Value);
						break;
					}
				case GStruct352.ESyncType.Hydration:
					{
						writer.Put(packet.Hydration.Value);
						break;
					}
				case GStruct352.ESyncType.Temperature:
					{
						writer.Put(packet.Temperature.Value);
						break;
					}
				case GStruct352.ESyncType.DamageCoeff:
					{
						writer.Put(packet.DamageCoeff.DamageCoeff);
						break;
					}
				case GStruct352.ESyncType.ApplyDamage:
					{
						writer.Put((int)packet.ApplyDamage.BodyPart);
						writer.Put(packet.ApplyDamage.Damage);
						writer.Put((int)packet.ApplyDamage.DamageType);
						break;
					}
				case GStruct352.ESyncType.DestroyedBodyPart:
					{
						writer.Put((int)packet.DestroyedBodyPart.BodyPart);
						writer.Put(packet.DestroyedBodyPart.IsDestroyed);
						if (packet.DestroyedBodyPart.IsDestroyed)
						{
							writer.Put((int)packet.DestroyedBodyPart.DamageType);
							break;
						}
						writer.Put(packet.DestroyedBodyPart.HealthMaximum);
						break;
					}
				case GStruct352.ESyncType.HealthRates:
					{
						writer.Put(packet.HealthRates.HealRate);
						writer.Put(packet.HealthRates.DamageRate);
						writer.Put(packet.HealthRates.DamageMultiplier);
						writer.Put(packet.HealthRates.Energy);
						writer.Put(packet.HealthRates.Hydration);
						writer.Put(packet.HealthRates.Temperature);
						break;
					}
				case GStruct352.ESyncType.HealerDone:
					{
						writer.Put(packet.HealerDone.EffectId);
						break;
					}
				case GStruct352.ESyncType.BurnEyes:
					{
						writer.Put(packet.BurnEyes.Position);
						writer.Put(packet.BurnEyes.DistanceStrength);
						writer.Put(packet.BurnEyes.NormalTime);
						break;
					}
				case GStruct352.ESyncType.Poison:
					{
						writer.Put(packet.Poison.Value);
						break;
					}
				case GStruct352.ESyncType.StaminaCoeff:
					{
						writer.Put(packet.StaminaCoeff.StaminaCoeff);
						break;
					}
			}
		}
	}
}
