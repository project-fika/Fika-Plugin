// © 2025 Lacyway All Rights Reserved

using Audio.SpatialSystem;
using Comfort.Common;
using Dissonance;
using Diz.Binding;
using EFT;
using EFT.Ballistics;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.Vaulting;
using EFT.Visual;
using Fika.Core.Coop.Custom;
using Fika.Core.Coop.Factories;
using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.ObservedClasses;
using Fika.Core.Coop.ObservedClasses.Snapshotting;
using Fika.Core.Coop.PacketHandlers;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using Fika.Core.Utils;
using HarmonyLib;
using JsonType;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;
using static Fika.Core.Networking.CommonSubPackets;
using static Fika.Core.Networking.SubPacket;
using static Fika.Core.Networking.SubPackets;
using static Fika.Core.UI.FikaUIGlobals;

namespace Fika.Core.Coop.Players
{
    /// <summary>
    /// Observed players are any other players in the world for a client, including bots. <br/>
    /// Bots are handled by the server, and other clients send their own data which the server replicates to other clients. <br/>
    /// As a host all <see cref="ObservedCoopPlayer"/>s are only other clients.
    /// </summary>
    public class ObservedCoopPlayer : CoopPlayer
    {
        #region Fields and Properties
        public FikaHealthBar HealthBar
        {
            get
            {
                return healthBar;
            }
        }
        public bool ShouldOverlap { get; internal set; }
        public override bool LeftStanceDisabled
        {
            get
            {
                return leftStancedDisabled;
            }
            internal set
            {
                if (leftStancedDisabled == value)
                {
                    return;
                }
                leftStancedDisabled = value;
                ShouldOverlap = true;
            }
        }
        public BetterSource VoipEftSource { get; set; }
        internal ObservedState CurrentPlayerState;

        private bool leftStancedDisabled;
        private FikaHealthBar healthBar = null;
        private Coroutine waitForStartRoutine;
        private bool isServer;
        private VoiceBroadcastTrigger voiceBroadcastTrigger;
        private GClass1050 soundSettings;
        private bool voipAssigned;
        private int frameSkip;

        public ObservedHealthController NetworkHealthController
        {
            get
            {
                return HealthController as ObservedHealthController;
            }
        }
        private readonly ObservedVaultingParametersClass ObservedVaultingParameters = new();
        public override bool CanBeSnapped
        {
            get
            {
                return false;
            }
        }
        public override EPointOfView PointOfView
        {
            get
            {
                return EPointOfView.ThirdPerson;
            }
            set
            {
                if (_playerBody.PointOfView.Value == value)
                {
                    return;
                }
                _playerBody.PointOfView.Value = value;
                CalculateScaleValueByFov((float)Singleton<SharedGameSettingsClass>.Instance.Game.Settings.FieldOfView);
                SetCompensationScale(false);
                PlayerBones.Ribcage.Original.localScale = new Vector3(1f, 1f, 1f);
                MovementContext.PlayerAnimatorPointOfView(value);
                BindableEvent pointOfViewChanged = PointOfViewChanged;
                pointOfViewChanged?.Invoke();
                _playerBody.UpdatePlayerRenders(_playerBody.PointOfView.Value, Side);
                ProceduralWeaponAnimation.PointOfView = value;
            }
        }
        public override AbstractHandsController HandsController
        {
            get
            {
                return base.HandsController;
            }

            set
            {
                base.HandsController = value;
                PlayerAnimator.EWeaponAnimationType weaponAnimationType = GetWeaponAnimationType(_handsController);
                MovementContext.PlayerAnimatorSetWeaponId(weaponAnimationType);
            }
        }
        public override Ray InteractionRay
        {
            get
            {
                Vector3 vector = HandsRotation * Vector3.forward;
                return new(_playerLookRaycastTransform.position, vector);
            }
        }
        public override float ProtagonistHearing
        {
            get
            {
                return Mathf.Max(1f, Singleton<BetterAudio>.Instance.ProtagonistHearing + 1f);
            }
        }
        private GClass896 cullingHandler;
        private readonly List<ObservedSlotViewHandler> observedSlotViewHandlers = [];
        #endregion

        public static async Task<ObservedCoopPlayer> CreateObservedPlayer(GameWorld gameWorld, int playerId, Vector3 position, Quaternion rotation, string layerName,
            string prefix, EPointOfView pointOfView, Profile profile, byte[] healthBytes, bool aiControl,
            EUpdateQueue updateQueue, EUpdateMode armsUpdateMode, EUpdateMode bodyUpdateMode,
            CharacterControllerSpawner.Mode characterControllerMode, Func<float> getSensitivity, Func<float> getAimingSensitivity,
            IViewFilter filter, MongoID firstId, ushort firstOperationId, bool isZombie)
        {
            bool useSimpleAnimator = isZombie;
#if DEBUG
            if (useSimpleAnimator)
            {
                FikaPlugin.Instance.FikaLogger.LogWarning("Using SimpleAnimator!");
            }
#endif
            ResourceKey resourceKey = useSimpleAnimator ? ResourceKeyManagerAbstractClass.ZOMBIE_BUNDLE_NAME : ResourceKeyManagerAbstractClass.PLAYER_BUNDLE_NAME;
            ObservedCoopPlayer player = Create<ObservedCoopPlayer>(gameWorld, resourceKey, playerId, position, updateQueue,
                armsUpdateMode, bodyUpdateMode, characterControllerMode, getSensitivity, getAimingSensitivity, prefix, aiControl, useSimpleAnimator);

            player.IsYourPlayer = false;
            player.IsObservedAI = aiControl;

            ObservedInventoryController inventoryController = new(player, profile, true, firstId, firstOperationId, aiControl);
            ObservedHealthController healthController = new(healthBytes, player, inventoryController, profile.Skills);

            ObservedStatisticsManager statisticsManager = new();
            ObservedQuestController observedQuestController = null;
            if (!aiControl)
            {
                observedQuestController = new(profile, inventoryController, null);
                observedQuestController.Init();
                observedQuestController.Run();
            }

            player.VoipState = (!FikaBackendUtils.IsHeadless && !aiControl &&
                !profile.IsHeadlessProfile() && Singleton<IFikaNetworkManager>.Instance.AllowVOIP) ? EVoipState.Available : EVoipState.NotAvailable;

            await player.Init(rotation, layerName, pointOfView, profile, inventoryController, healthController,
                statisticsManager, observedQuestController, null, null, filter, player.VoipState, aiControl, false);

            player._handsController = EmptyHandsController.smethod_6<EmptyHandsController>(player);
            player._handsController.Spawn(1f, delegate { });

            player.AIData = new GClass567(null, player);

            Traverse observedTraverse = Traverse.Create(player);
            observedTraverse.Field<GClass896>("gclass896_0").Value = new();
            player.cullingHandler = observedTraverse.Field<GClass896>("gclass896_0").Value;
            player.cullingHandler.Initialize(player, player.PlayerBones);
            if (FikaBackendUtils.IsHeadless || profile.IsPlayerProfile())
            {
                player.cullingHandler.Disable();
            }

            if (!aiControl)
            {
                HashSet<ETraderServiceType> services = Traverse.Create(player).Field<HashSet<ETraderServiceType>>("hashSet_0").Value;
                foreach (ETraderServiceType etraderServiceType in Singleton<BackendConfigSettingsClass>.Instance.ServicesData.Keys)
                {
                    services.Add(etraderServiceType);
                }
            }

            player.AggressorFound = false;
            player._animators[0].enabled = true;
            player.isServer = FikaBackendUtils.IsServer;
            player.Snapshotter = new(player);
            player.CurrentPlayerState = new(position, player.Rotation);

            if (GClass2762.int_1 == 0)
            {
                GClass2762.int_1 = 1;
                player.frameSkip = 1;
            }
            else
            {
                GClass2762.int_1 = 0;
                player.frameSkip = 0;
            }

            return player;
        }

        public override void InitVoip(EVoipState voipState)
        {
            if (voipState == EVoipState.Available)
            {
                FikaGlobals.LogInfo($"Initializing VOIP for {Profile.Nickname}");
                SetupVoiceBroadcastTrigger();
                DissonanceComms = DissonanceComms.Instance;
                if (DissonanceComms != null)
                {
                    DissonanceComms.TrackPlayerPosition(this);
                    if (VoipAudioSource != null)
                    {
                        SourceBindingCreated();
                    }
                    else
                    {
                        FikaGlobals.LogError($"VoipAudioSource was null when attempting to initialize VOIP for {Profile.Nickname}");
                    }
                }
                else
                {
                    FikaGlobals.LogError($"DissonanceComms was null when attempting to initialize VOIP for {Profile.Nickname}");
                }
            }
        }

        private void SourceBindingCreated()
        {
            if (voipAssigned)
            {
                return;
            }
            VoipEftSource = MonoBehaviourSingleton<BetterAudio>.Instance.CreateBetterSource<SimpleSource>(
                VoipAudioSource, BetterAudio.AudioSourceGroupType.Voip, true, true);
            if (VoipEftSource == null)
            {
                FikaGlobals.LogError($"Could not initialize VoipEftSource for {Profile.Nickname}");
                return;
            }
            VoipEftSource.SetMixerGroup(MonoBehaviourSingleton<BetterAudio>.Instance.ObservedPlayerSpeechMixer);
            VoipEftSource.SetRolloff(60f);
            MonoBehaviourSingleton<SpatialAudioSystem>.Instance.ProcessSourceOcclusion(this, VoipEftSource, false);
            voipAssigned = true;
        }

        private void SetupVoiceBroadcastTrigger()
        {
            voiceBroadcastTrigger = gameObject.AddComponent<VoiceBroadcastTrigger>();
            voiceBroadcastTrigger.ChannelType = CommTriggerTarget.Self;
            soundSettings = Singleton<SharedGameSettingsClass>.Instance.Sound.Settings;
            CompositeDisposable.BindState(soundSettings.VoipDeviceSensitivity, ChangeVoipDeviceSensitivity);
        }

        private void ChangeVoipDeviceSensitivity(int value)
        {
            float num = (float)value / 100f;
            voiceBroadcastTrigger.ActivationFader.Volume = num;
        }

        public override BasePhysicalClass CreatePhysical()
        {
            return new BasePhysicalClass();
        }

        public override bool CheckSurface(float range)
        {
            float spreadRange = 42f * ProtagonistHearing;
            return !(Distance - spreadRange > 0);
        }

        public override void Say(EPhraseTrigger phrase, bool demand = false, float delay = 0, ETagStatus mask = 0, int probability = 100, bool aggressive = false)
        {
            if (gameObject.activeSelf)
            {
                base.Say(phrase, demand, delay, mask, probability, aggressive);
            }
        }

        public override void PlayGroundedSound(float fallHeight, float jumpHeight)
        {
            (bool hit, BaseBallistic.ESurfaceSound surfaceSound) = method_73();
            method_74(hit, surfaceSound);
            base.PlayGroundedSound(fallHeight, jumpHeight);
        }

        public override void OnSkillLevelChanged(AbstractSkillClass skill)
        {
            // Do nothing
        }

        public override void OnWeaponMastered(MasterSkillClass masterSkill)
        {
            // Do nothing
        }

        public override void StartInflictSelfDamageCoroutine()
        {
            // Do nothing
        }

        public override void AddStateSpeedLimit(float speedDelta, ESpeedLimit cause)
        {
            // Do nothing
        }

        public override void UpdateSpeedLimit(float speedDelta, ESpeedLimit cause)
        {
            // Do nothing
        }

        public override void UpdateSpeedLimitByHealth()
        {
            // Do nothing
        }

        public override void UpdateSpeedLimit(float speedDelta, ESpeedLimit cause, float duration)
        {
            // Do nothing
        }

        public override void UpdatePhones()
        {
            // Do nothing
        }

        public override void FaceshieldMarkOperation(FaceShieldComponent armor, bool hasServerOrigin)
        {
            // Do nothing
        }

        public override void ManageAggressor(DamageInfoStruct DamageInfo, EBodyPart bodyPart, EBodyPartColliderType colliderType)
        {
            if (_isDeadAlready)
            {
                return;
            }
            if (!HealthController.IsAlive)
            {
                _isDeadAlready = true;
            }
            if (DamageInfo.Player == null)
            {
                return;
            }
            Player player = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(DamageInfo.Player.iPlayer.ProfileId);
            if (player == this)
            {
                return;
            }
            if (DamageInfo.Weapon != null)
            {
                player.ExecuteShotSkill(DamageInfo.Weapon);
            }

            if (player.IsYourPlayer)
            {
                // Check for GClass increment
                bool flag = DamageInfo.DidBodyDamage / HealthController.GetBodyPartHealth(bodyPart, false).Maximum >= 0.6f && HealthController.FindExistingEffect<GInterface314>(bodyPart) != null;
                player.StatisticsManager.OnEnemyDamage(DamageInfo, bodyPart, ProfileId, Side, Profile.Info.Settings.Role,
                    GroupId, HealthController.GetBodyPartHealth(EBodyPart.Common, false).Maximum, flag,
                    Vector3.Distance(player.Transform.position, Transform.position), CurrentHour,
                    Inventory.EquippedInSlotsTemplateIds, HealthController.BodyPartEffects, TriggerZones);
                return;
            }
        }

        public override void UpdateArmsCondition()
        {
            // Do nothing
        }

        public override bool ShouldVocalizeDeath(EBodyPart bodyPart)
        {
            return bodyPart > EBodyPart.Head;
        }

        public override void SendHeadlightsPacket(bool isSilent)
        {
            // Do nothing
        }

        public override void ApplyHitDebuff(float damage, float staminaBurnRate, EBodyPart bodyPartType, EDamageType damageType)
        {
            if (damageType.IsEnemyDamage())
            {
                IncreaseAwareness(20f);
            }
            if (HealthController.IsAlive && (!MovementContext.PhysicalConditionIs(EPhysicalCondition.OnPainkillers) || damage > 4f) && !IsAI)
            {
                if (gameObject.activeSelf && Speaker != null)
                {
                    Speaker.Play(EPhraseTrigger.OnBeingHurt, HealthStatus, true, null);
                }
            }
        }

        public void HandleExplosive(DamageInfoStruct DamageInfo, EBodyPart bodyPartType, EBodyPartColliderType colliderType)
        {
            if (HealthController.DamageCoeff == 0)
            {
                return;
            }

            LastDamagedBodyPart = bodyPartType;
            LastBodyPart = bodyPartType;
            LastDamageInfo = DamageInfo;
            LastDamageType = DamageInfo.DamageType;

            DamagePacket packet = new()
            {
                NetId = NetId,
                Damage = DamageInfo.Damage,
                DamageType = DamageInfo.DamageType,
                BodyPartType = bodyPartType,
                ColliderType = colliderType,
                Direction = DamageInfo.Direction,
                Point = DamageInfo.HitPoint,
                HitNormal = DamageInfo.HitNormal,
                PenetrationPower = DamageInfo.PenetrationPower,
                SourceId = DamageInfo.SourceId,
                WeaponId = DamageInfo.Weapon != null ? DamageInfo.Weapon.Id : string.Empty
            };
            PacketSender.SendPacket(ref packet);
        }

        public override void ApplyDamageInfo(DamageInfoStruct DamageInfo, EBodyPart bodyPartType, EBodyPartColliderType colliderType, float absorbed)
        {
            LastAggressor = DamageInfo.Player.iPlayer;
            LastDamagedBodyPart = bodyPartType;
            LastBodyPart = bodyPartType;
            LastDamageInfo = DamageInfo;
            LastDamageType = DamageInfo.DamageType;
        }

        public ShotInfoClass HandleSniperShot(DamageInfoStruct DamageInfo, EBodyPart bodyPartType, EBodyPartColliderType colliderType, EArmorPlateCollider armorPlateCollider, ShotIdStruct shotId)
        {
            if (HealthController.DamageCoeff == 0)
            {
                return null;
            }

            ApplyHitDebuff(DamageInfo.Damage, 0f, bodyPartType, DamageInfo.DamageType);
            LastDamagedBodyPart = bodyPartType;
            LastBodyPart = bodyPartType;
            LastDamageInfo = DamageInfo;
            LastDamageType = DamageInfo.DamageType;

            DamagePacket packet = new()
            {
                NetId = NetId,
                Damage = DamageInfo.Damage,
                DamageType = DamageInfo.DamageType,
                BodyPartType = bodyPartType,
                ColliderType = colliderType,
                ArmorPlateCollider = armorPlateCollider,
                Direction = DamageInfo.Direction,
                Point = DamageInfo.HitPoint,
                HitNormal = DamageInfo.HitNormal,
                PenetrationPower = DamageInfo.PenetrationPower,
                BlockedBy = DamageInfo.BlockedBy,
                DeflectedBy = DamageInfo.DeflectedBy,
                SourceId = DamageInfo.SourceId,
                ArmorDamage = DamageInfo.ArmorDamage,
                WeaponId = DamageInfo.Weapon.Id
            };
            PacketSender.SendPacket(ref packet);

            return new()
            {
                PoV = EPointOfView.ThirdPerson,
                Penetrated = DamageInfo.Penetrated,
                Material = MaterialType.Body
            };
        }

        public override ShotInfoClass ApplyShot(DamageInfoStruct damageInfo, EBodyPart bodyPartType, EBodyPartColliderType colliderType, EArmorPlateCollider armorPlateCollider, ShotIdStruct shotId)
        {
            if (HealthController != null && !HealthController.IsAlive)
            {
                return null;
            }

            ShotReactions(damageInfo, bodyPartType);
            ApplyHitDebuff(damageInfo.Damage, 0f, bodyPartType, damageInfo.DamageType);
            bool flag = !string.IsNullOrEmpty(damageInfo.DeflectedBy);
            float damage = damageInfo.Damage;
            List<ArmorComponent> list = ProceedDamageThroughArmor(ref damageInfo, colliderType, armorPlateCollider, true);
            if (list != null)
            {
                _preAllocatedArmorComponents.Clear();
                _preAllocatedArmorComponents.AddRange(list);
                SendArmorDamagePacket();
            }
            MaterialType materialType = flag ? MaterialType.HelmetRicochet : ((_preAllocatedArmorComponents == null || _preAllocatedArmorComponents.Count < 1)
                ? MaterialType.Body : _preAllocatedArmorComponents[0].Material);
            ShotInfoClass hitInfo = new()
            {
                PoV = PointOfView,
                Penetrated = string.IsNullOrEmpty(damageInfo.BlockedBy) || string.IsNullOrEmpty(damageInfo.DeflectedBy),
                Material = materialType
            };
            float num = damage - damageInfo.Damage;
            if (num > 0)
            {
                damageInfo.DidArmorDamage = num;
            }
            damageInfo.DidBodyDamage = damageInfo.Damage;
            ReceiveDamage(damageInfo.Damage, bodyPartType, damageInfo.DamageType, num, hitInfo.Material);

            DamagePacket packet = new()
            {
                NetId = NetId,
                Damage = damageInfo.Damage,
                DamageType = damageInfo.DamageType,
                BodyPartType = bodyPartType,
                ColliderType = colliderType,
                ArmorPlateCollider = armorPlateCollider,
                Direction = damageInfo.Direction,
                Point = damageInfo.HitPoint,
                HitNormal = damageInfo.HitNormal,
                PenetrationPower = damageInfo.PenetrationPower,
                BlockedBy = damageInfo.BlockedBy,
                DeflectedBy = damageInfo.DeflectedBy,
                SourceId = damageInfo.SourceId,
                ArmorDamage = damageInfo.ArmorDamage,
                ProfileId = damageInfo.Player.iPlayer.ProfileId,
                Material = materialType,
                WeaponId = damageInfo.Weapon.Id
            };
            PacketSender.SendPacket(ref packet);

            // Run this to get weapon skill
            ManageAggressor(damageInfo, bodyPartType, colliderType);

            return hitInfo;
        }

        public override void ApplyExplosionDamageToArmor(Dictionary<ExplosiveHitArmorColliderStruct, float> armorDamage, DamageInfoStruct DamageInfo)
        {
            if (isServer)
            {
                _preAllocatedArmorComponents.Clear();
                List<ArmorComponent> listToCheck = [];
                Inventory.GetPutOnArmorsNonAlloc(listToCheck);
                foreach (ArmorComponent armorComponent in listToCheck)
                {
                    float num = 0f;
                    foreach (KeyValuePair<ExplosiveHitArmorColliderStruct, float> keyValuePair in armorDamage)
                    {
                        if (armorComponent.ShotMatches(keyValuePair.Key.BodyPartColliderType, keyValuePair.Key.ArmorPlateCollider))
                        {
                            num += keyValuePair.Value;
                            _preAllocatedArmorComponents.Add(armorComponent);
                        }
                    }
                    if (num > 0f)
                    {
                        num = armorComponent.ApplyExplosionDurabilityDamage(num, DamageInfo, _preAllocatedArmorComponents);
                        method_92(num, armorComponent);
                    }
                }

                if (_preAllocatedArmorComponents.Count > 0)
                {
                    SendArmorDamagePacket();
                }
            }
        }

        public ShotInfoClass ApplyClientShot(DamageInfoStruct damageInfo, EBodyPart bodyPartType, EBodyPartColliderType colliderType, EArmorPlateCollider armorPlateCollider, ShotIdStruct shotId)
        {
            ShotReactions(damageInfo, bodyPartType);
            ApplyHitDebuff(damageInfo.Damage, 0f, bodyPartType, damageInfo.DamageType);
            LastAggressor = damageInfo.Player.iPlayer;
            LastDamagedBodyPart = bodyPartType;
            LastBodyPart = bodyPartType;
            LastDamageInfo = damageInfo;
            LastDamageType = damageInfo.DamageType;

            if (HealthController != null && !HealthController.IsAlive)
            {
                return null;
            }

            bool flag = !string.IsNullOrEmpty(damageInfo.DeflectedBy);
            float damage = damageInfo.Damage;
            List<ArmorComponent> list = ProceedDamageThroughArmor(ref damageInfo, colliderType, armorPlateCollider, true);
            if (list != null)
            {
                _preAllocatedArmorComponents.Clear();
                _preAllocatedArmorComponents.AddRange(list);
                SendArmorDamagePacket();
            }
            MaterialType materialType = flag ? MaterialType.HelmetRicochet : (_preAllocatedArmorComponents.Count < 1 ? MaterialType.Body : _preAllocatedArmorComponents[0].Material);
            ShotInfoClass hitInfo = new()
            {
                PoV = PointOfView,
                Penetrated = string.IsNullOrEmpty(damageInfo.BlockedBy) || string.IsNullOrEmpty(damageInfo.DeflectedBy),
                Material = materialType
            };
            float num = damage - damageInfo.Damage;
            if (num > 0)
            {
                damageInfo.DidArmorDamage = num;
            }
            damageInfo.DidBodyDamage = damageInfo.Damage;
            ReceiveDamage(damageInfo.Damage, bodyPartType, damageInfo.DamageType, num, hitInfo.Material);

            DamagePacket packet = new()
            {
                NetId = NetId,
                Damage = damageInfo.Damage,
                DamageType = damageInfo.DamageType,
                BodyPartType = bodyPartType,
                ColliderType = colliderType,
                ArmorPlateCollider = armorPlateCollider,
                Direction = damageInfo.Direction,
                Point = damageInfo.HitPoint,
                HitNormal = damageInfo.HitNormal,
                PenetrationPower = damageInfo.PenetrationPower,
                BlockedBy = damageInfo.BlockedBy,
                DeflectedBy = damageInfo.DeflectedBy,
                SourceId = damageInfo.SourceId,
                ArmorDamage = damageInfo.ArmorDamage,
                ProfileId = damageInfo.Player.iPlayer.ProfileId,
                Material = materialType,
                WeaponId = damageInfo.Weapon.Id
            };
            PacketSender.SendPacket(ref packet);

            // Run this to get weapon skill
            ManageAggressor(damageInfo, bodyPartType, colliderType);

            return hitInfo;
        }

        public override void OnMounting(MountingPacketStruct.EMountingCommand command)
        {
            // Do nothing
        }

        public override void ApplyCorpseImpulse()
        {
            if (cullingHandler.IsVisible || isServer)
            {
                if (CorpseSyncPacket.BodyPartColliderType != EBodyPartColliderType.None
                        && PlayerBones.BodyPartCollidersDictionary.TryGetValue(CorpseSyncPacket.BodyPartColliderType, out BodyPartCollider bodyPartCollider))
                {
                    Corpse.Ragdoll.ApplyImpulse(bodyPartCollider.Collider, CorpseSyncPacket.Direction, CorpseSyncPacket.Point, CorpseSyncPacket.Force);
                }
            }
        }

        public override void CreateMovementContext()
        {
            LayerMask movement_MASK = EFTHardSettings.Instance.MOVEMENT_MASK;
            MovementContext = ObservedMovementContext.Create(this, GetBodyAnimatorCommon, GetCharacterControllerCommon, movement_MASK);
        }

        public override void OnHealthEffectAdded(IEffect effect)
        {
            // Check for GClass increments
            if (effect is GInterface316 fracture && !fracture.WasPaused && FractureSound != null && Singleton<BetterAudio>.Instantiated)
            {
                Singleton<BetterAudio>.Instance.PlayAtPoint(Position, FractureSound, CameraClass.Instance.Distance(Position),
                    BetterAudio.AudioSourceGroupType.Impacts, 15, 0.7f, EOcclusionTest.Fast, null, false);
            }
        }

        public override void OnHealthEffectRemoved(IEffect effect)
        {
            // Do nothing
        }

        public override void ConnectSkillManager()
        {
            // Do nothing
        }

        #region proceed
        public override void Proceed(KnifeComponent knife, Callback<IKnifeController> callback, bool scheduled = true)
        {
            HandsControllerFactory factory = new(this, knifeComponent: knife);
            Func<KnifeController> func = new(factory.CreateObservedKnifeController);
            new Process<KnifeController, IKnifeController>(this, func, factory.KnifeComponent.Item)
                .method_0(null, callback, scheduled);
        }

        public override void Proceed(ThrowWeapItemClass throwWeap, Callback<IHandsThrowController> callback, bool scheduled = true)
        {
            HandsControllerFactory factory = new(this, throwWeap);
            Func<GrenadeHandsController> func = new(factory.CreateObservedGrenadeController);
            new Process<GrenadeHandsController, IHandsThrowController>(this, func, throwWeap, false)
                .method_0(null, callback, scheduled);
        }

        public override void Proceed(ThrowWeapItemClass throwWeap, Callback<GInterface179> callback, bool scheduled = true)
        {
            HandsControllerFactory factory = new(this, throwWeap);
            Func<QuickGrenadeThrowHandsController> func = new(factory.CreateObservedQuickGrenadeController);
            new Process<QuickGrenadeThrowHandsController, GInterface179>(this, func, throwWeap, false)
                .method_0(null, callback, scheduled);
        }

        public override void Proceed(Weapon weapon, Callback<IFirearmHandsController> callback, bool scheduled = true)
        {
            HandsControllerFactory factory = new(this, weapon);
            Func<FirearmController> func = new(factory.CreateObservedFirearmController);
            new Process<FirearmController, IFirearmHandsController>(this, func, factory.Item, true)
                .method_0(null, callback, scheduled);
        }

        public override void Proceed(MedsItemClass meds, GStruct353<EBodyPart> bodyParts, Callback<GInterface176> callback, int animationVariant, bool scheduled = true)
        {
            HandsControllerFactory factory = new(this)
            {
                MedsItem = meds,
                BodyParts = bodyParts,
                AnimationVariant = animationVariant
            };
            Func<MedsController> func = new(factory.CreateObservedMedsController);
            new Process<MedsController, GInterface176>(this, func, meds, false)
                .method_0(null, callback, scheduled);
        }

        public override void Proceed(FoodDrinkItemClass foodDrink, float amount, Callback<GInterface176> callback, int animationVariant, bool scheduled = true)
        {
            HandsControllerFactory factory = new(this)
            {
                FoodItem = foodDrink,
                Amount = amount,
                AnimationVariant = animationVariant
            };
            Func<MedsController> func = new(factory.CreateObservedMedsController);
            new Process<MedsController, GInterface176>(this, func, foodDrink, false)
                .method_0(null, callback, scheduled);
        }
        #endregion

        public override void OnFovUpdatedEvent(int fov)
        {
            // Do nothing
        }

        public override void ShowHelloNotification(string sender)
        {
            // Do nothing
        }

        public override void HealthControllerUpdate(float deltaTime)
        {
            // Do nothing
        }

        public override void OnPhraseTold(EPhraseTrigger @event, TaggedClip clip, TagBank bank, PhraseSpeakerClass speaker)
        {
            method_33(clip);
        }

        public override void MouseLook(bool forceApplyToOriginalRibcage = false)
        {
            if (HandsController != null)
            {
                MovementContext.RotationAction?.Invoke(this);
            }
        }

        public void Interpolate(ref PlayerStatePacket to, ref PlayerStatePacket from, double ratio)
        {
            float interpolateRatio = (float)ratio;
            bool isJumpSet = MovementContext.PlayerAnimatorIsJumpSetted();

            method_74(to.HasGround, to.SurfaceSound);

            Rotation = new Vector2(Mathf.LerpAngle(from.Rotation.x, to.Rotation.x, interpolateRatio),
                Mathf.LerpUnclamped(from.Rotation.y, to.Rotation.y, interpolateRatio));

            if (to.HeadRotation != default)
            {
                Vector3 newRotation = Vector3.LerpUnclamped(HeadRotation, to.HeadRotation, interpolateRatio);
                HeadRotation = newRotation;
                ProceduralWeaponAnimation.SetHeadRotation(newRotation);
            }

            bool isGrounded = to.IsGrounded;
            MovementContext.IsGrounded = isGrounded;

            EPlayerState newState = to.State;

            if (newState == EPlayerState.Jump)
            {
                MovementContext.PlayerAnimatorEnableJump(true);
                if (isServer)
                {
                    MovementContext.method_2(1f);
                }
            }

            if (isJumpSet && isGrounded)
            {
                MovementContext.PlayerAnimatorEnableJump(false);
                MovementContext.PlayerAnimatorEnableLanding(true);
            }
            if (CurrentStateName == EPlayerState.Sprint && newState == EPlayerState.Transition)
            {
                MovementContext.UpdateSprintInertia();
                MovementContext.PlayerAnimatorEnableInert(false);
            }

            Physical.SerializationStruct = to.Stamina;

            if (!Mathf.Approximately(MovementContext.Step, to.Step))
            {
                CurrentManagedState.SetStep(to.Step);
            }

            if (MovementContext.IsSprintEnabled != to.IsSprinting)
            {
                CurrentManagedState.EnableSprint(to.IsSprinting);
            }

            if (MovementContext.IsInPronePose != to.IsProne)
            {
                MovementContext.IsInPronePose = to.IsProne;
            }

            if (!Mathf.Approximately(PoseLevel, to.PoseLevel))
            {
                MovementContext.SetPoseLevel(from.PoseLevel + (to.PoseLevel - from.PoseLevel));
            }

            MovementContext.SetCurrentClientAnimatorStateIndex(to.AnimatorStateIndex);
            MovementContext.SetCharacterMovementSpeed(to.CharacterMovementSpeed, true);

            if (MovementContext.BlindFire != to.Blindfire)
            {
                MovementContext.SetBlindFire(to.Blindfire);
            }

            if (!IsInventoryOpened && isGrounded)
            {
                Move(to.MovementDirection);
                if (isServer)
                {
                    MovementContext.method_1(to.MovementDirection);
                }
            }

            Transform.position = Vector3.LerpUnclamped(from.Position, to.Position, interpolateRatio);

            float currentTilt = MovementContext.Tilt;
            if (!Mathf.Approximately(currentTilt, to.Tilt))
            {
                float newTilt = Mathf.LerpUnclamped(currentTilt, to.Tilt, interpolateRatio);
                MovementContext.SetTilt(newTilt, true);
            }

            if (!ObservedOverlap.ApproxEquals(to.WeaponOverlap))
            {
                ObservedOverlap = to.WeaponOverlap;
                ShouldOverlap = true;
            }
            LeftStanceDisabled = to.LeftStanceDisabled;
        }

        public void ManualStateUpdate()
        {
            bool isJumpSet = MovementContext.PlayerAnimatorIsJumpSetted();

            method_74(CurrentPlayerState.HasGround, CurrentPlayerState.SurfaceSound);

            Rotation = CurrentPlayerState.Rotation;

            HeadRotation = CurrentPlayerState.HeadRotation;
            ProceduralWeaponAnimation.SetHeadRotation(CurrentPlayerState.HeadRotation);

            bool isGrounded = CurrentPlayerState.IsGrounded;
            MovementContext.IsGrounded = isGrounded;

            EPlayerState newState = CurrentPlayerState.State;

            if (newState == EPlayerState.Jump)
            {
                MovementContext.PlayerAnimatorEnableJump(true);
                if (isServer)
                {
                    MovementContext.method_2(1f);
                }
            }

            if (isJumpSet && isGrounded)
            {
                MovementContext.PlayerAnimatorEnableJump(false);
                MovementContext.PlayerAnimatorEnableLanding(true);
            }
            if (CurrentStateName == EPlayerState.Sprint && newState == EPlayerState.Transition)
            {
                MovementContext.UpdateSprintInertia();
                MovementContext.PlayerAnimatorEnableInert(false);
            }

            Physical.SerializationStruct = CurrentPlayerState.Stamina;

            if (MovementContext.Step != CurrentPlayerState.Step)
            {
                CurrentManagedState.SetStep(CurrentPlayerState.Step);
            }

            if (MovementContext.IsSprintEnabled != CurrentPlayerState.IsSprinting)
            {
                CurrentManagedState.EnableSprint(CurrentPlayerState.IsSprinting);
            }

            if (MovementContext.IsInPronePose != CurrentPlayerState.IsProne)
            {
                MovementContext.IsInPronePose = CurrentPlayerState.IsProne;
            }

            if (!Mathf.Approximately(PoseLevel, CurrentPlayerState.PoseLevel))
            {
                MovementContext.SetPoseLevel(CurrentPlayerState.PoseLevel);
            }

            MovementContext.SetCurrentClientAnimatorStateIndex(CurrentPlayerState.AnimatorStateIndex);
            MovementContext.SetCharacterMovementSpeed(CurrentPlayerState.CharacterMovementSpeed, true);

            if (MovementContext.BlindFire != CurrentPlayerState.Blindfire)
            {
                MovementContext.SetBlindFire(CurrentPlayerState.Blindfire);
            }

            if (!IsInventoryOpened && isGrounded)
            {
                Move(CurrentPlayerState.MovementDirection);
                if (isServer)
                {
                    MovementContext.method_1(CurrentPlayerState.MovementDirection);
                }
            }

            Transform.position = CurrentPlayerState.Position;

            if (!Mathf.Approximately(MovementContext.Tilt, CurrentPlayerState.Tilt))
            {
                MovementContext.SetTilt(CurrentPlayerState.Tilt, true);
            }

            if (!ObservedOverlap.ApproxEquals(CurrentPlayerState.WeaponOverlap))
            {
                ObservedOverlap = CurrentPlayerState.WeaponOverlap;
                ShouldOverlap = true;
            }
            LeftStanceDisabled = CurrentPlayerState.LeftStanceDisabled;
        }

        public override void InteractionRaycast()
        {
            if (_playerLookRaycastTransform == null || !HealthController.IsAlive)
            {
                return;
            }

            InteractableObjectIsProxy = false;
            Ray interactionRay = InteractionRay;
            Boolean_0 = false;
            GameObject gameObject = GameWorld.FindInteractable(interactionRay, out _);
            if (gameObject != null)
            {
                Player player = gameObject.GetComponent<Player>();
                if (player != null && player != InteractablePlayer)
                {
                    InteractablePlayer = (player != this) ? player : null;
                }
                return;
            }

            InteractablePlayer = null;
        }

        public override Corpse CreateCorpse()
        {
            if (CorpseSyncPacket.InventoryDescriptor != null)
            {
                SetInventory(CorpseSyncPacket.InventoryDescriptor);
            }
            if (FikaBackendUtils.IsClient)
            {
                ObservedCorpse observedCorpse = CreateCorpse<ObservedCorpse>(CorpseSyncPacket.OverallVelocity);
                observedCorpse.IsZombieCorpse = UsedSimplifiedSkeleton;
                observedCorpse.SetSpecificSettings(PlayerBones.RightPalm);
                Singleton<GameWorld>.Instance.ObservedPlayersCorpses.Add(NetId, observedCorpse);
                return observedCorpse;
            }

            Corpse corpse = CreateCorpse<Corpse>(CorpseSyncPacket.OverallVelocity);
            corpse.IsZombieCorpse = UsedSimplifiedSkeleton;
            //CorpsePositionSyncer.Create(corpse.gameObject, corpse, NetId);
            return corpse;
        }

        public override void OnDead(EDamageType damageType)
        {
            if (HealthBar != null)
            {
                Destroy(HealthBar);
            }

            if (FikaPlugin.ShowNotifications.Value)
            {
                if (!IsObservedAI)
                {
                    string nickname = !string.IsNullOrEmpty(Profile.Info.MainProfileNickname) ? Profile.Info.MainProfileNickname : Profile.Nickname;
                    if (damageType != EDamageType.Undefined)
                    {
                        NotificationManagerClass.DisplayWarningNotification(string.Format(LocaleUtils.GROUP_MEMBER_DIED_FROM.Localized(),
                            [ColorizeText(EColor.GREEN, nickname), ColorizeText(EColor.RED, ("DamageType_" + damageType.ToString()).Localized())]));
                    }
                    else
                    {
                        NotificationManagerClass.DisplayWarningNotification(string.Format(LocaleUtils.GROUP_MEMBER_DIED.Localized(),
                            ColorizeText(EColor.GREEN, nickname)));
                    }
                }
                if (LocaleUtils.IsBoss(Profile.Info.Settings.Role, out string name) && IsObservedAI && LastAggressor != null)
                {
                    if (LastAggressor is CoopPlayer aggressor)
                    {
                        string aggressorNickname = !string.IsNullOrEmpty(LastAggressor.Profile.Info.MainProfileNickname) ? LastAggressor.Profile.Info.MainProfileNickname : LastAggressor.Profile.Nickname;
                        if (aggressor.gameObject.name.StartsWith("Player_") || aggressor.IsYourPlayer)
                        {
                            NotificationManagerClass.DisplayMessageNotification(string.Format(LocaleUtils.KILLED_BOSS.Localized(),
                            [ColorizeText(EColor.GREEN, LastAggressor.Profile.Info.MainProfileNickname), ColorizeText(EColor.BROWN, name)]),
                            iconType: EFT.Communications.ENotificationIconType.Friend);
                        }
                    }
                }
            }
            Singleton<BetterAudio>.Instance.ProtagonistHearingChanged -= UpdateSoundRolloff;
            base.OnDead(damageType);
            if (cullingHandler != null)
            {
                cullingHandler.DisableCullingOnDead();
            }
            if (CorpseSyncPacket.ItemInHands != null)
            {
                Corpse.SetItemInHandsLootedCallback(null);
                Corpse.ItemInHands.Value = CorpseSyncPacket.ItemInHands;
                Corpse.SetItemInHandsLootedCallback(ReleaseHand);
            }
            CorpseSyncPacket = default;
            Snapshotter.Clear();
            Singleton<IFikaNetworkManager>.Instance.ObservedCoopPlayers.Remove(this);
        }

        public override void vmethod_3(TransitControllerAbstractClass controller, int transitPointId, string keyId, EDateTime time)
        {
            // Do nothing
        }

        public override void HandleDamagePacket(DamagePacket packet)
        {
            DamageInfoStruct DamageInfo = new()
            {
                Damage = packet.Damage,
                DamageType = packet.DamageType,
                BodyPartColliderType = packet.ColliderType,
                HitPoint = packet.Point,
                HitNormal = packet.HitNormal,
                Direction = packet.Direction,
                PenetrationPower = packet.PenetrationPower,
                BlockedBy = packet.BlockedBy,
                DeflectedBy = packet.DeflectedBy,
                SourceId = packet.SourceId,
                ArmorDamage = packet.ArmorDamage
            };

            if (!string.IsNullOrEmpty(packet.ProfileId))
            {
                IPlayerOwner player = Singleton<GameWorld>.Instance.GetAlivePlayerBridgeByProfileID(packet.ProfileId);

                if (player != null)
                {
                    DamageInfo.Player = player;
                    LastAggressor = player.iPlayer;
                    if (IsYourPlayer)
                    {
                        if (!FikaPlugin.Instance.FriendlyFire && DamageInfo.Player.iPlayer.GroupId == GroupId)
                        {
                            return;
                        }
                    }
                }

                /*// TODO: Fix this and consistently get the correct data...
				if (Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(packet.ProfileId).HandsController.Item is Weapon weapon)
				{
					DamageInfo.Weapon = weapon;
				}*/
                lastWeaponId = packet.WeaponId;
            }

            ShotReactions(DamageInfo, packet.BodyPartType);
            ReceiveDamage(DamageInfo.Damage, packet.BodyPartType, DamageInfo.DamageType, packet.Absorbed, packet.Material);

            LastDamageInfo = DamageInfo;
            LastBodyPart = packet.BodyPartType;
            LastDamagedBodyPart = packet.BodyPartType;
        }

        public override void OnBeenKilledByAggressor(IPlayer aggressor, DamageInfoStruct damageInfo, EBodyPart bodyPart, EDamageType lethalDamageType)
        {
            // Only handle if it was ourselves as otherwise it's irrelevant
            if (LastAggressor.IsYourPlayer)
            {
                base.OnBeenKilledByAggressor(aggressor, damageInfo, bodyPart, lethalDamageType);
                return;
            }

            if (aggressor.GroupId == "Fika" && !aggressor.IsYourPlayer)
            {
                CoopPlayer mainPlayer = (CoopPlayer)Singleton<GameWorld>.Instance.MainPlayer;
                if (mainPlayer == null)
                {
                    return;
                }

                if (!mainPlayer.HealthController.IsAlive)
                {
                    return;
                }

                WildSpawnType role = Profile.Info.Settings.Role;
                bool countAsBoss = role.CountAsBossForStatistics() && !(role is WildSpawnType.pmcUSEC or WildSpawnType.pmcBEAR);
                int experience = Profile.Info.Settings.Experience;
                SessionCountersClass sessionCounters = mainPlayer.Profile.EftStats.SessionCounters;
                HandleSharedExperience(countAsBoss, experience, sessionCounters);

                if (FikaPlugin.Instance.SharedQuestProgression && FikaPlugin.EasyKillConditions.Value)
                {
#if DEBUG
                    FikaPlugin.Instance.FikaLogger.LogInfo("Handling teammate kill from teammate: " + aggressor.Profile.Nickname);
#endif

                    float distance = Vector3.Distance(aggressor.Position, Position);
                    mainPlayer.HandleTeammateKill(damageInfo, bodyPart, Side, role, ProfileId,
                        distance, Inventory.EquippedInSlotsTemplateIds, HealthController.BodyPartEffects, TriggerZones,
                        (CoopPlayer)aggressor);
                }
            }
        }

        public override void ExternalInteraction()
        {
            // Do nothing
        }

        public void SetInventory(GClass1693 inventoryDescriptor)
        {
            if (HandsController != null)
            {
                HandsController.FastForwardCurrentState();
            }

            Inventory inventory = new GClass1685()
            {
                Equipment = inventoryDescriptor,
            }.ToInventory();

            InventoryController.ReplaceInventory(inventory);
            if (CorpseSyncPacket.ItemSlot <= EquipmentSlot.Scabbard)
            {
                Item heldItem = Equipment.GetSlot(CorpseSyncPacket.ItemSlot).ContainedItem;
                if (heldItem != null)
                {
                    CorpseSyncPacket.ItemInHands = heldItem;
                }
            }

            if (!FikaBackendUtils.IsHeadless)
            {
                RefreshSlotViews();
            }
        }

        private void RefreshSlotViews()
        {
            foreach (EquipmentSlot equipmentSlot in PlayerBody.SlotNames)
            {
                Slot slot = Inventory.Equipment.GetSlot(equipmentSlot);
                ObservedSlotViewHandler handler = new(slot, this, equipmentSlot);
                observedSlotViewHandlers.Add(handler);
            }

            if (PlayerBody.HaveHolster && PlayerBody.SlotViews.ContainsKey(EquipmentSlot.Holster))
            {
                Slot slot = Inventory.Equipment.GetSlot(EquipmentSlot.Holster);
                ObservedSlotViewHandler handler = new(slot, this, EquipmentSlot.Holster);
                observedSlotViewHandlers.Add(handler);
            }

            if (HandsController != null && HandsController is CoopObservedFirearmController controller)
            {
                if (Inventory.Equipment.TryFindItem(controller.Weapon.Id, out Item item))
                {
                    if (item is not Weapon newWeapon)
                    {
                        FikaPlugin.Instance.FikaLogger.LogError("SetInventory::HandsController item was not Weapon");
                        return;
                    }

                    IEnumerable<Slot> newSlots = newWeapon.AllSlots;
                    if (newSlots != null)
                    {
                        Dictionary<string, GClass746.GClass747> currentViews = [];
                        foreach (KeyValuePair<EFT.InventoryLogic.IContainer, GClass746.GClass747> kvp in controller.CCV.ContainerBones)
                        {
                            if (kvp.Key is Slot slot && slot.ContainedItem != null)
                            {
                                if (currentViews.ContainsKey(slot.FullId))
                                {
                                    FikaPlugin.Instance.FikaLogger.LogError("RefreshSlotViews::CRITICAL ERROR DICTIONARY: " + slot.FullId);
                                    continue;
                                }
                                currentViews.Add(slot.FullId, kvp.Value);
                            }
                        }
                        controller.CCV.RemoveBones(controller.Weapon.AllSlots);
                        foreach (EFT.InventoryLogic.IContainer container in newSlots)
                        {
                            if (container is Slot slot)
                            {
                                if (slot.ContainedItem == null)
                                {
                                    Transform transform = GClass819.FindTransformRecursive(controller.CCV.GameObject.transform, slot.ID, true);
                                    if (transform == null)
                                    {
                                        FikaPlugin.Instance.FikaLogger.LogWarning("RefreshSlotViews::Transform was missing: " + slot.ID);
                                        continue;
                                    }
                                    controller.CCV.AddBone(slot, transform);
                                    continue;
                                }
                                foreach (KeyValuePair<string, GClass746.GClass747> kvp in currentViews)
                                {
                                    if (kvp.Key == slot.FullId)
                                    {
                                        controller.CCV.ContainerBones[slot] = kvp.Value;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public override void DoObservedVault(VaultPacket packet)
        {
            if (packet.VaultingStrategy != EVaultingStrategy.Vault)
            {
                if (packet.VaultingStrategy != EVaultingStrategy.Climb)
                {
                    return;
                }
                MovementContext.PlayerAnimator.SetDoClimb(true);
            }
            else
            {
                MovementContext.PlayerAnimator.SetDoVault(true);
            }

            ObservedVaultingParameters.MaxWeightPointPosition = packet.VaultingPoint;
            ObservedVaultingParameters.VaultingHeight = packet.VaultingHeight;
            ObservedVaultingParameters.VaultingLength = packet.VaultingLength;
            ObservedVaultingParameters.VaultingSpeed = packet.VaultingSpeed;
            ObservedVaultingParameters.AbsoluteForwardVelocity = packet.AbsoluteForwardVelocity;
            ObservedVaultingParameters.BehindObstacleRatio = packet.BehindObstacleHeight;

            MovementContext.PlayerAnimator.SetVaultingSpeed(packet.VaultingSpeed);
            MovementContext.PlayerAnimator.SetVaultingHeight(packet.VaultingHeight);
            MovementContext.PlayerAnimator.SetVaultingLength(packet.VaultingLength);
            MovementContext.PlayerAnimator.SetBehindObstacleRatio(packet.BehindObstacleHeight);
            MovementContext.PlayerAnimator.SetAbsoluteForwardVelocity(packet.AbsoluteForwardVelocity);

            MovementContext.PlayerAnimator.SetIsGrounded(true);
        }

        public void InitObservedPlayer(bool isHeadlessClient)
        {
            PacketSender = gameObject.AddComponent<ObservedPacketSender>();
            Traverse playerTraverse = Traverse.Create(this);

            if (IsObservedAI)
            {
                BotStatePacket packet = new()
                {
                    NetId = NetId,
                    Type = BotStatePacket.EStateType.LoadBot
                };

                PacketSender.Client.SendData(ref packet, LiteNetLib.DeliveryMethod.ReliableOrdered);

                IVaultingComponent vaultingComponent = playerTraverse.Field<IVaultingComponent>("_vaultingComponent").Value;
                if (vaultingComponent != null)
                {
                    UpdateEvent -= vaultingComponent.DoVaultingTick;
                }

                playerTraverse.Field("_vaultingComponent").SetValue(null);
                playerTraverse.Field("_vaultingComponentDebug").SetValue(null);
                playerTraverse.Field("_vaultingParameters").SetValue(null);
                playerTraverse.Field("_vaultingGameplayRestrictions").SetValue(null);
                playerTraverse.Field("_vaultAudioController").SetValue(null);
                playerTraverse.Field("_sprintVaultAudioController").SetValue(null);
                playerTraverse.Field("_climbAudioController").SetValue(null);
            }

            if (!IsObservedAI)
            {
                if (!isHeadlessClient)
                {
                    Profile.Info.GroupId = "Fika";
                    Profile.Info.TeamId = "Fika";
                    waitForStartRoutine = StartCoroutine(CreateHealthBar());
                }

                IVaultingComponent vaultingComponent = playerTraverse.Field<IVaultingComponent>("_vaultingComponent").Value;
                if (vaultingComponent != null)
                {
                    UpdateEvent -= vaultingComponent.DoVaultingTick;
                }
                playerTraverse.Field("_vaultingComponent").SetValue(null);
                playerTraverse.Field("_vaultingComponentDebug").SetValue(null);
                playerTraverse.Field("_vaultingParameters").SetValue(null);
                playerTraverse.Field("_vaultingGameplayRestrictions").SetValue(null);

                InitVaultingAudioControllers(ObservedVaultingParameters);

                if (FikaPlugin.ShowNotifications.Value && !isHeadlessClient)
                {
                    NotificationManagerClass.DisplayMessageNotification(string.Format(LocaleUtils.GROUP_MEMBER_SPAWNED.Localized(),
                        ColorizeText(EColor.GREEN, Profile.Info.MainProfileNickname)),
                    EFT.Communications.ENotificationDurationType.Default, EFT.Communications.ENotificationIconType.Friend);
                }
            }
        }

        private IEnumerator CreateHealthBar()
        {
            CoopGame coopGame = CoopGame.Instance;
            if (coopGame == null)
            {
                yield break;
            }

            while (coopGame.Status != GameStatus.Started)
            {
                yield return null;
            }

            healthBar = FikaHealthBar.Create(this);

            while (Singleton<GameWorld>.Instance.MainPlayer == null)
            {
                yield return null;
            }
            Singleton<GameWorld>.Instance.MainPlayer.StatisticsManager.OnGroupMemberConnected(Inventory);

            yield break;
        }

        public override void LateUpdate()
        {
            DistanceDirty = true;
            OcclusionDirty = true;
            if (UpdateQueue == EUpdateQueue.FixedUpdate)
            {
                return;
            }
            if (HealthController == null || !HealthController.IsAlive)
            {
                return;
            }
            Physical.LateUpdate();
            VisualPass();
            PropUpdate();
            _armsupdated = false;
            _bodyupdated = false;
            if (_nFixedFrames > 0)
            {
                _nFixedFrames = 0;
                _fixedTime = 0f;
            }
        }

        public override void UpdateMuffledState()
        {
            // Do nothing
        }

        public override void SendVoiceMuffledState(bool isMuffled)
        {
            // Do nothing
        }

        public void SetMuffledState(bool muffled)
        {
            Muffled = muffled;
            if (MonoBehaviourSingleton<BetterAudio>.Instantiated)
            {
                BetterAudio instance = MonoBehaviourSingleton<BetterAudio>.Instance;
                AudioMixerGroup audioMixerGroup = Muffled ? instance.SimpleOccludedMixerGroup : instance.ObservedPlayerSpeechMixer;
                if (SpeechSource != null)
                {
                    SpeechSource.SetMixerGroup(audioMixerGroup);
                }
                if (VoipEftSource != null)
                {
                    VoipEftSource.SetMixerGroup(audioMixerGroup);
                }
            }
        }

        public override void LandingAdjustments(float d)
        {
            // Do nothing
        }

        public new void CreateCompass()
        {
            bool compassInstantiated = Traverse.Create(this).Field<bool>("_compassInstantiated").Value;
            if (!compassInstantiated)
            {
                Transform transform = Singleton<PoolManagerClass>.Instance.CreateFromPool<Transform>(new ResourceKey
                {
                    path = "assets/content/weapons/additional_hands/item_compass.bundle"
                });
                transform.SetParent(PlayerBones.Ribcage.Original, false);
                transform.localRotation = Quaternion.identity;
                transform.localPosition = Vector3.zero;
                method_27(transform.gameObject);
                Traverse.Create(this).Field("_compassInstantiated").SetValue(true);
                return;
            }
        }

        public override void OnAnimatedInteraction(EInteraction interaction)
        {
            if (interaction == EInteraction.FriendlyGesture)
            {
                InteractionRaycast();
                if (InteractablePlayer != null)
                {
                    InteractablePlayer.ShowHelloNotification(Profile.Nickname);
                }
            }
        }

        public override void PauseAllEffectsOnPlayer()
        {
            NetworkHealthController.PauseAllEffects();
        }

        public override void UnpauseAllEffectsOnPlayer()
        {
            NetworkHealthController.UnpauseAllEffects();
        }

        public override void OnVaulting()
        {
            // Do nothing
        }

        public override void ManualUpdate(float deltaTime, float? platformDeltaTime = null, int loop = 1)
        {
            _bodyupdated = true;
            _bodyTime = deltaTime;

            method_13(deltaTime);

            if (Time.frameCount % 2 == frameSkip)
            {
                UpdateTriggerColliderSearcher(deltaTime, cullingHandler.IsCloseToMyPlayerCamera);
            }
            cullingHandler.ManualUpdate(deltaTime);
        }

        public override void InitAudioController()
        {
            base.InitAudioController();
            Singleton<BetterAudio>.Instance.ProtagonistHearingChanged += UpdateSoundRolloff;
        }

        private void UpdateSoundRolloff()
        {
            method_64(CommonAssets.Scripts.Audio.EAudioMovementState.Run);
            UpdateVoiceSoundRolloff();
        }

        private void UpdateVoiceSoundRolloff()
        {
            SpeechSource?.SetRolloff(60f * ProtagonistHearing);
        }

        public override bool UpdateGrenadeAnimatorDuePoV()
        {
            return true;
        }

        public override void FixedUpdateTick()
        {
            // Do nothing
        }

        public override void OnDestroy()
        {
            if (HandsController != null)
            {
                AbstractHandsController handsController = HandsController;
                if (handsController != null && handsController.ControllerGameObject != null)
                {
                    HandsController.OnGameSessionEnd();
                    HandsController.Destroy();
                }
            }
            if (HealthBar != null)
            {
                Destroy(HealthBar);
            }
            if (Singleton<BetterAudio>.Instantiated)
            {
                Singleton<BetterAudio>.Instance.ProtagonistHearingChanged -= UpdateSoundRolloff;
            }
            foreach (ObservedSlotViewHandler slotViewHandler in observedSlotViewHandlers)
            {
                slotViewHandler.Dispose();
            }
            observedSlotViewHandlers.Clear();
            if (HealthController.IsAlive)
            {
                if (!Singleton<IFikaNetworkManager>.Instance.ObservedCoopPlayers.Remove(this))
                {
                    FikaGlobals.LogWarning($"Failed to remove {ProfileId}, {Profile.Nickname} from observed list");
                }
            }
            base.OnDestroy();
        }

        public override void SendHandsInteractionStateChanged(bool value, int animationId)
        {
            if (value)
            {
                MovementContext.SetBlindFire(0);
            }
        }

        public void HandleProceedPacket(ref ProceedPacket packet)
        {
            switch (packet.ProceedType)
            {
                case EProceedType.EmptyHands:
                    {
                        CreateEmptyHandsController();
                        break;
                    }
                case EProceedType.FoodClass:
                case EProceedType.MedsClass:
                    {
                        CreateMedsController(packet.ItemId, packet.BodyParts, packet.Amount, packet.AnimationVariant);
                        break;
                    }
                case EProceedType.GrenadeClass:
                    {
                        CreateGrenadeController(packet.ItemId);
                        break;
                    }
                case EProceedType.QuickGrenadeThrow:
                    {
                        CreateQuickGrenadeController(packet.ItemId);
                        break;
                    }
                case EProceedType.QuickKnifeKick:
                    {
                        CreateQuickKnifeController(packet.ItemId);
                        break;
                    }
                case EProceedType.QuickUse:
                    {
                        CreateQuickUseItemController(packet.ItemId);
                        break;
                    }
                case EProceedType.Weapon:
                    {
                        CreateFirearmController(packet.ItemId);
                        break;
                    }
                case EProceedType.Knife:
                    {
                        CreateKnifeController(packet.ItemId);
                        break;
                    }
                case EProceedType.UsableItem:
                    {
                        CreateUsableItemController(packet.ItemId);
                        break;
                    }
                case EProceedType.Stationary:
                    {
                        CreateFirearmController(packet.ItemId, true);
                        break;
                    }
            }
        }

        private class ObservedSlotViewHandler : IDisposable
        {
            private readonly Slot slot;
            private readonly ObservedCoopPlayer observedPlayer;
            private readonly EquipmentSlot slotType;

            public ObservedSlotViewHandler(Slot itemSlot, ObservedCoopPlayer player, EquipmentSlot equipmentType)
            {
                slot = itemSlot;
                observedPlayer = player;
                slotType = equipmentType;

                itemSlot.OnAddOrRemoveItem += HandleItemMove;
            }

            public void Dispose()
            {
                slot.OnAddOrRemoveItem -= HandleItemMove;
            }

            private void HandleItemMove(Item item)
            {
                Transform slotBone = observedPlayer.PlayerBody.GetSlotBone(slotType);
                Transform alternativeHolsterBone = observedPlayer.PlayerBody.GetAlternativeHolsterBone(slotType);
                PlayerBody.GClass2119 newSlotView = new(observedPlayer.PlayerBody, slot, slotBone, slotType,
                        observedPlayer.Inventory.Equipment.GetSlot(EquipmentSlot.Backpack), alternativeHolsterBone, false);
                PlayerBody.GClass2119 oldSlotView = observedPlayer.PlayerBody.SlotViews.AddOrReplace(slotType, newSlotView);
                if (oldSlotView != null)
                {
                    ClearSlotView(oldSlotView);
                    oldSlotView.Dispose();
                }
                observedPlayer.PlayerBody.ValidateHoodedDress(slotType);
                GlobalEventHandlerClass.Instance.CreateCommonEvent<GClass3345>().Invoke(observedPlayer.ProfileId);
                Dispose();
            }

            private void ClearSlotView(PlayerBody.GClass2119 oldSlotView)
            {
                for (int i = 0; i < oldSlotView.Renderers.Length; i++)
                {
                    oldSlotView.Renderers[i].forceRenderingOff = false;
                }
                if (oldSlotView.Dresses != null)
                {
                    Dress[] dresses = oldSlotView.Dresses;
                    for (int j = 0; j < dresses.Length; j++)
                    {
                        GStruct58 bodyRenderer = dresses[j].GetBodyRenderer();
                        for (int k = 0; k < bodyRenderer.Renderers.Length; k++)
                        {
                            bodyRenderer.Renderers[k].forceRenderingOff = false;
                        }
                    }
                }
            }
        }

        #region handControllers
        private void CreateHandsController(Func<AbstractHandsController> controllerFactory, Item item)
        {
            CreateHandsControllerHandler handler = new((item != null) ? method_131(item) : null);

            handler.setInHandsOperation?.Confirm(true);

            if (HandsController != null)
            {
                AbstractHandsController handsController = HandsController;
                HandsController.FastForwardCurrentState();
                if (HandsController != handsController && HandsController != null)
                {
                    HandsController.FastForwardCurrentState();
                }
                HandsController.Destroy();
                if (HandsController != null)
                {
                    Destroy(HandsController);
                }
                HandsController = null;
            }

            base.SpawnController(controllerFactory(), handler.DisposeHandler);
            if (PlayerBody != null)
            {
                PlayerBody.GClass2119 slotByItem = PlayerBody.GetSlotViewByItem(item);
                if (slotByItem != null)
                {
                    slotByItem.DestroyCurrentModel();
                }
            }
        }

        public void SpawnHandsController(EHandsControllerType controllerType, string itemId, bool isStationary)
        {
            switch (controllerType)
            {
                case EHandsControllerType.Empty:
                    CreateEmptyHandsController();
                    break;
                case EHandsControllerType.Firearm:
                    CreateFirearmController(itemId, isStationary, true);
                    break;
                case EHandsControllerType.Meds:
                    CreateMedsController(itemId, new(EBodyPart.Head), 0f, 1);
                    break;
                case EHandsControllerType.Grenade:
                    CreateGrenadeController(itemId);
                    break;
                case EHandsControllerType.Knife:
                    CreateKnifeController(itemId);
                    break;
                case EHandsControllerType.QuickGrenade:
                    CreateQuickGrenadeController(itemId);
                    break;
                case EHandsControllerType.QuickKnife:
                    CreateQuickKnifeController(itemId);
                    break;
                case EHandsControllerType.QuickUseItem:
                    CreateQuickUseItemController(itemId);
                    break;
                case EHandsControllerType.UsableItem:
                    CreateUsableItemController(itemId);
                    break;
                default:
                    FikaPlugin.Instance.FikaLogger.LogWarning($"ObservedCoopPlayer::SpawnHandsController: Unhandled ControllerType, was {controllerType}");
                    break;
            }
        }

        private void CreateEmptyHandsController()
        {
            CreateHandsController(ReturnEmptyHandsController, null);
        }

        private AbstractHandsController ReturnEmptyHandsController()
        {
            return CoopObservedEmptyHandsController.Create(this);
        }

        private void CreateFirearmController(string itemId, bool isStationary = false, bool initial = false)
        {
            CreateFirearmControllerHandler handler = new(this);

            if (isStationary)
            {
                if (initial)
                {
                    handler.item = Singleton<GameWorld>.Instance.FindStationaryWeaponByItemId(itemId).Item;
                    CreateHandsController(handler.ReturnController, handler.item);
                    FastForwardToStationaryWeapon(handler.item, MovementContext.Rotation, Transform.rotation, Transform.rotation);
                    return;
                }
                handler.item = Singleton<GameWorld>.Instance.FindStationaryWeaponByItemId(itemId).Item;
                CreateHandsController(handler.ReturnController, handler.item);
                return;
            }
            GStruct457<Item> result = FindItemById(itemId, false, false);
            if (!result.Succeeded)
            {
                FikaPlugin.Instance.FikaLogger.LogError(result.Error);
                return;
            }
            handler.item = result.Value;
            CreateHandsController(handler.ReturnController, handler.item);
        }

        private void CreateGrenadeController(string itemId)
        {
            CreateGrenadeControllerHandler handler = new(this);

            GStruct457<Item> result = FindItemById(itemId, false, false);
            if (!result.Succeeded)
            {
                FikaPlugin.Instance.FikaLogger.LogError(result.Error);
                return;
            }
            handler.item = result.Value;
            if (handler.item is ThrowWeapItemClass)
            {
                CreateHandsController(handler.ReturnController, handler.item);
            }
            else
            {
                FikaPlugin.Instance.FikaLogger.LogError($"CreateGrenadeController: Item was not of type GrenadeClass, was {handler.item.GetType()}!");
            }
        }

        private void CreateMedsController(string itemId, GStruct353<EBodyPart> bodyParts, float amount, int animationVariant)
        {
            GStruct457<Item> result = FindItemById(itemId, false, false);
            if (!result.Succeeded)
            {
                FikaPlugin.Instance.FikaLogger.LogError(result.Error);
                return;
            }
            CreateMedsControllerHandler handler = new(this, result.Value, bodyParts, amount, animationVariant);
            CreateHandsController(handler.ReturnController, handler.item);
        }

        private void CreateKnifeController(string itemId)
        {
            CreateKnifeControllerHandler handler = new(this);
            GStruct457<Item> result = FindItemById(itemId, false, false);
            if (!result.Succeeded)
            {
                FikaPlugin.Instance.FikaLogger.LogError(result.Error);
                return;
            }
            handler.knife = result.Value.GetItemComponent<KnifeComponent>();
            if (handler.knife != null)
            {
                CreateHandsController(handler.ReturnController, handler.knife.Item);
            }
            else
            {
                FikaPlugin.Instance.FikaLogger.LogError($"CreateKnifeController: Item did not contain a KnifeComponent, was of type {handler.knife.GetType()}!");
            }
        }

        private void CreateQuickGrenadeController(string itemId)
        {
            CreateQuickGrenadeControllerHandler handler = new(this);
            GStruct457<Item> result = FindItemById(itemId, false, false);
            if (!result.Succeeded)
            {
                FikaPlugin.Instance.FikaLogger.LogError(result.Error);
                return;
            }
            handler.item = result.Value;
            if (handler.item is ThrowWeapItemClass)
            {
                CreateHandsController(handler.ReturnController, handler.item);
            }
            else
            {
                FikaPlugin.Instance.FikaLogger.LogError($"CreateQuickGrenadeController: Item was not of type GrenadeClass, was {handler.item.GetType()}!");
            }
        }

        private void CreateQuickKnifeController(string itemId)
        {
            CreateQuickKnifeControllerHandler handler = new(this);
            GStruct457<Item> result = FindItemById(itemId, false, false);
            if (!result.Succeeded)
            {
                FikaPlugin.Instance.FikaLogger.LogError(result.Error);
                return;
            }
            handler.knife = result.Value.GetItemComponent<KnifeComponent>();
            if (handler.knife != null)
            {
                CreateHandsController(handler.ReturnController, handler.knife.Item);
            }
            else
            {
                FikaPlugin.Instance.FikaLogger.LogError($"CreateQuickKnifeController: Item did not contain a KnifeComponent, was of type {handler.knife.GetType()}!");
            }
        }

        private void CreateUsableItemController(string itemId)
        {
            GStruct457<Item> result = FindItemById(itemId, false, false);
            if (!result.Succeeded)
            {
                FikaPlugin.Instance.FikaLogger.LogError(result.Error);
                return;
            }
            CreateUsableItemControllerHandler handler = new(this, result.Value);
            CreateHandsController(handler.ReturnController, handler.item);
        }

        private void CreateQuickUseItemController(string itemId)
        {
            GStruct457<Item> result = FindItemById(itemId, false, false);
            if (!result.Succeeded)
            {
                FikaPlugin.Instance.FikaLogger.LogError(result.Error);
                return;
            }
            CreateQuickUseItemControllerHandler handler = new(this, result.Value);
            CreateHandsController(handler.ReturnController, handler.item);
        }

        public void SetAggressorData(string killerId, EBodyPart bodyPart, string weaponId)
        {
            Player killer = Singleton<GameWorld>.Instance.GetEverExistedPlayerByID(killerId);
            if (killer != null)
            {
                LastAggressor = killer;
            }
            LastBodyPart = bodyPart;
            lastWeaponId = weaponId;

            if (LastDamageInfo.Weapon is null && !string.IsNullOrEmpty(lastWeaponId))
            {
                FindKillerWeapon();
            }
        }

        private class RemoveHandsControllerHandler(ObservedCoopPlayer coopPlayer, Callback callback)
        {
            private readonly ObservedCoopPlayer coopPlayer = coopPlayer;
            private readonly Callback callback = callback;

            public void Handle(Result<GInterface160> result)
            {
                if (coopPlayer._removeFromHandsCallback == callback)
                {
                    coopPlayer._removeFromHandsCallback = null;
                }
                callback.Invoke(result);
            }
        }

        private class CreateHandsControllerHandler(Class1193 setInHandsOperation)
        {
            public readonly Class1193 setInHandsOperation = setInHandsOperation;

            internal void DisposeHandler()
            {
                Class1193 handler = setInHandsOperation;
                if (handler == null)
                {
                    return;
                }
                handler.Dispose();
            }
        }

        private class CreateFirearmControllerHandler(ObservedCoopPlayer coopPlayer)
        {
            private readonly ObservedCoopPlayer coopPlayer = coopPlayer;
            public Item item;

            internal AbstractHandsController ReturnController()
            {
                return CoopObservedFirearmController.Create(coopPlayer, (Weapon)item);
            }
        }

        private class CreateGrenadeControllerHandler(ObservedCoopPlayer coopPlayer)
        {
            private readonly ObservedCoopPlayer coopPlayer = coopPlayer;
            public Item item;

            internal AbstractHandsController ReturnController()
            {
                return CoopObservedGrenadeController.Create(coopPlayer, (ThrowWeapItemClass)item);
            }
        }

        private class CreateMedsControllerHandler(ObservedCoopPlayer coopPlayer, Item item, GStruct353<EBodyPart> bodyParts, float amount, int animationVariant)
        {
            private readonly ObservedCoopPlayer coopPlayer = coopPlayer;
            public readonly Item item = item;
            private readonly GStruct353<EBodyPart> bodyParts = bodyParts;
            private readonly float amount = amount;
            private readonly int animationVariant = animationVariant;

            internal AbstractHandsController ReturnController()
            {
                return CoopObservedMedsController.Create(coopPlayer, item, bodyParts, amount, animationVariant);
            }
        }

        private class CreateKnifeControllerHandler(ObservedCoopPlayer coopPlayer)
        {
            private readonly ObservedCoopPlayer coopPlayer = coopPlayer;
            public KnifeComponent knife;

            internal AbstractHandsController ReturnController()
            {
                return CoopObservedKnifeController.Create(coopPlayer, knife);
            }
        }

        private class CreateQuickGrenadeControllerHandler(ObservedCoopPlayer coopPlayer)
        {
            private readonly ObservedCoopPlayer coopPlayer = coopPlayer;
            public Item item;

            internal AbstractHandsController ReturnController()
            {
                return CoopObservedQuickGrenadeController.Create(coopPlayer, (ThrowWeapItemClass)item);
            }
        }

        private class CreateQuickKnifeControllerHandler(ObservedCoopPlayer coopPlayer)
        {
            private readonly ObservedCoopPlayer coopPlayer = coopPlayer;
            public KnifeComponent knife;

            internal AbstractHandsController ReturnController()
            {
                return QuickKnifeKickController.smethod_9<QuickKnifeKickController>(coopPlayer, knife);
            }
        }

        private class CreateUsableItemControllerHandler(ObservedCoopPlayer coopPlayer, Item item)
        {
            private readonly ObservedCoopPlayer coopPlayer = coopPlayer;
            public readonly Item item = item;

            internal AbstractHandsController ReturnController()
            {
                return UsableItemController.smethod_6<UsableItemController>(coopPlayer, item);
            }
        }

        private class CreateQuickUseItemControllerHandler(ObservedCoopPlayer coopPlayer, Item item)
        {
            private readonly ObservedCoopPlayer coopPlayer = coopPlayer;
            public readonly Item item = item;

            internal AbstractHandsController ReturnController()
            {
                return QuickUseItemController.smethod_6<QuickUseItemController>(coopPlayer, item);
            }
        }
    }
}

#endregion