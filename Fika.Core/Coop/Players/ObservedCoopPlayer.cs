// © 2025 Lacyway All Rights Reserved

using Audio.SpatialSystem;
using Comfort.Common;
using Dissonance;
using Diz.Binding;
using EFT;
using EFT.AssetsManager;
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
using RootMotion.FinalIK;
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
                return _healthBar;
            }
        }
        public bool ShouldOverlap { get; internal set; }
        public override bool LeftStanceDisabled
        {
            get
            {
                return _leftStancedDisabled;
            }
            internal set
            {
                if (_leftStancedDisabled == value)
                {
                    return;
                }
                _leftStancedDisabled = value;
                ShouldOverlap = true;
            }
        }
        public BetterSource VoipEftSource { get; set; }
        internal ObservedState CurrentPlayerState;

        private bool _leftStancedDisabled;
        private FikaHealthBar _healthBar;
        private Coroutine _waitForStartRoutine;
        private bool _isServer;
        private VoiceBroadcastTrigger _voiceBroadcastTrigger;
        private SoundSettingsDataClass _soundSettings;
        private bool _voipAssigned;
        private int _frameSkip;

        public ObservedHealthController NetworkHealthController
        {
            get
            {
                return HealthController as ObservedHealthController;
            }
        }
        private readonly ObservedVaultingParametersClass _observedVaultingParameters = new();
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
        public float TurnOffFbbikAt = 0f;
        private float _lastDistance = 0f;
        private LocalPlayerCullingHandlerClass _cullingHandler;
        private float _rightHand;
        private float _leftHand;
        private LimbIK[] _observedLimbs;
        private Transform[] _observedMarkers;
        private bool _shouldCullController;
        private readonly List<ObservedSlotViewHandler> _observedSlotViewHandlers = [];
        private ObservedCorpseCulling _observedCorpseCulling;
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
                observedQuestController = new(profile, inventoryController, inventoryController.PlayerSearchController, null);
                observedQuestController.Init();
                observedQuestController.Run();
            }

            player.VoipState = (!FikaBackendUtils.IsHeadless && !aiControl && Singleton<IFikaNetworkManager>.Instance.AllowVOIP)
                ? EVoipState.Available : EVoipState.NotAvailable;

            await player.Init(rotation, layerName, pointOfView, profile, inventoryController, healthController,
                statisticsManager, observedQuestController, null,
                null, filter, player.VoipState, aiControl, false);

            player.Pedometer.Stop();
            player._handsController = EmptyHandsController.smethod_6<EmptyHandsController>(player);
            player._handsController.Spawn(1f, FikaGlobals.EmptyAction);

            player.AIData = new PlayerAIDataClass(null, player);

            Traverse observedTraverse = Traverse.Create(player);
            observedTraverse.Field<LocalPlayerCullingHandlerClass>("localPlayerCullingHandlerClass").Value = new();
            player._cullingHandler = observedTraverse.Field<LocalPlayerCullingHandlerClass>("localPlayerCullingHandlerClass").Value;
            player._cullingHandler.Initialize(player, player.PlayerBones);

            if (FikaBackendUtils.IsHeadless || profile.IsPlayerProfile())
            {
                player._cullingHandler.Disable();
            }

            if (FikaBackendUtils.IsHeadless)
            {
                player.EnabledAnimators = EAnimatorMask.Thirdperson | EAnimatorMask.Arms;
            }

            if (!aiControl)
            {
                HashSet<ETraderServiceType> services = Traverse.Create(player).Field<HashSet<ETraderServiceType>>("hashSet_0").Value;
                foreach (ETraderServiceType etraderServiceType in Singleton<BackendConfigSettingsClass>.Instance.ServicesData.Keys)
                {
                    services.Add(etraderServiceType);
                }
            }

            player._observedLimbs = player.GetComponent<PlayerPoolObject>().LimbIks;
            player._observedMarkers = observedTraverse.Field<Transform[]>("_markers").Value;

            player.AggressorFound = false;
            player._animators[0].enabled = true;
            player._isServer = FikaBackendUtils.IsServer;
            player.Snapshotter = new(player);
            player.CurrentPlayerState = new(position, player.Rotation);

            if (GClass2828.Int_1 == 0)
            {
                GClass2828.Int_1 = 1;
                player._frameSkip = 1;
            }
            else
            {
                GClass2828.Int_1 = 0;
                player._frameSkip = 0;
            }

            CameraClass.Instance.FoVUpdateAction -= player.OnFovUpdatedEvent;

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
            if (_voipAssigned)
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
            _voipAssigned = true;
        }

        private void SetupVoiceBroadcastTrigger()
        {
            _voiceBroadcastTrigger = gameObject.AddComponent<VoiceBroadcastTrigger>();
            _voiceBroadcastTrigger.ChannelType = CommTriggerTarget.Self;
            _soundSettings = Singleton<SharedGameSettingsClass>.Instance.Sound.Settings;
            CompositeDisposable.BindState(_soundSettings.VoipDeviceSensitivity, ChangeVoipDeviceSensitivity);
        }

        private void ChangeVoipDeviceSensitivity(int value)
        {
            float num = (float)value / 100f;
            _voiceBroadcastTrigger.ActivationFader.Volume = num;
        }

        public override BasePhysicalClass CreatePhysical()
        {
            return new BasePhysicalClass();
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
            (bool hit, BaseBallistic.ESurfaceSound surfaceSound) = method_75();
            method_76(hit, surfaceSound);
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

        public override void ShotReactions(DamageInfoStruct shot, EBodyPart bodyPart)
        {
            TurnOffFbbikAt = Time.time + 0.6f;
            base.ShotReactions(shot, bodyPart);
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
                bool flag = DamageInfo.DidBodyDamage / HealthController.GetBodyPartHealth(bodyPart, false).Maximum >= 0.6f && HealthController.FindExistingEffect<GInterface323>(bodyPart) != null;
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
            if (_isServer)
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
                        method_96(num, armorComponent);
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
            if (_cullingHandler.IsVisible || _isServer)
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
            if (effect is GInterface325 fracture && !fracture.WasPaused && FractureSound != null && Singleton<BetterAudio>.Instantiated)
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

        public override void Proceed(ThrowWeapItemClass throwWeap, Callback<GInterface188> callback, bool scheduled = true)
        {
            HandsControllerFactory factory = new(this, throwWeap);
            Func<QuickGrenadeThrowHandsController> func = new(factory.CreateObservedQuickGrenadeController);
            new Process<QuickGrenadeThrowHandsController, GInterface188>(this, func, throwWeap, false)
                .method_0(null, callback, scheduled);
        }

        public override void Proceed(Weapon weapon, Callback<IFirearmHandsController> callback, bool scheduled = true)
        {
            HandsControllerFactory factory = new(this, weapon);
            Func<FirearmController> func = new(factory.CreateObservedFirearmController);
            new Process<FirearmController, IFirearmHandsController>(this, func, factory.Item, true)
                .method_0(null, callback, scheduled);
        }

        public override void Proceed(MedsItemClass meds, GStruct375<EBodyPart> bodyParts, Callback<GInterface185> callback, int animationVariant, bool scheduled = true)
        {
            HandsControllerFactory factory = new(this)
            {
                MedsItem = meds,
                BodyParts = bodyParts,
                AnimationVariant = animationVariant
            };
            Func<MedsController> func = new(factory.CreateObservedMedsController);
            new Process<MedsController, GInterface185>(this, func, meds, false)
                .method_0(null, callback, scheduled);
        }

        public override void Proceed(FoodDrinkItemClass foodDrink, float amount, Callback<GInterface185> callback, int animationVariant, bool scheduled = true)
        {
            HandsControllerFactory factory = new(this)
            {
                FoodItem = foodDrink,
                Amount = amount,
                AnimationVariant = animationVariant
            };
            Func<MedsController> func = new(factory.CreateObservedMedsController);
            new Process<MedsController, GInterface185>(this, func, foodDrink, false)
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
            if (!FikaBackendUtils.IsHeadless)
            {
                method_33(clip);
            }
        }

        public override void MouseLook(bool forceApplyToOriginalRibcage = false)
        {
            MovementContext.RotationAction.Invoke(this);
        }

        public override bool CheckSurface(float range)
        {
            if (_lastDistance > (range * ProtagonistHearing))
            {
                return false;
            }

            (bool hit, BaseBallistic.ESurfaceSound surfaceSound) = method_75();
            method_76(hit, surfaceSound);
            if (Environment == EnvironmentType.Outdoor)
            {
                method_35();
            }
            return true;
        }

        /// <summary>
        /// Updates replicated values from the <see cref="ObservedState"/>
        /// </summary>
        public void ManualStateUpdate()
        {
            bool isJumpSet = MovementContext.PlayerAnimatorIsJumpSetted();

            Rotation = CurrentPlayerState.Rotation;

            HeadRotation = CurrentPlayerState.HeadRotation;
            ProceduralWeaponAnimation.SetHeadRotation(CurrentPlayerState.HeadRotation);

            bool isGrounded = CurrentPlayerState.IsGrounded;
            MovementContext.IsGrounded = isGrounded;

            EPlayerState newState = CurrentPlayerState.State;

            if (newState == EPlayerState.Jump)
            {
                MovementContext.PlayerAnimatorEnableJump(true);
                if (_isServer)
                {
                    MovementContext.method_2(1f);
                }
            }

            if (isJumpSet && isGrounded)
            {
                MovementContext.PlayerAnimatorEnableJump(false);
                MovementContext.PlayerAnimatorEnableLanding(true);
                if (newState is EPlayerState.Run or EPlayerState.Sprint)
                {
                    MovementContext.PlayerAnimatorEnableInert(true);
                }
                else
                {
                    MovementContext.PlayerAnimatorEnableInert(false);
                }
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

            MovementContext.SetCharacterMovementSpeed(CurrentPlayerState.MovementSpeed, true);
            MovementContext.SprintSpeed = CurrentPlayerState.SprintSpeed;

            if (MovementContext.BlindFire != CurrentPlayerState.Blindfire)
            {
                MovementContext.SetBlindFire(CurrentPlayerState.Blindfire);
            }

            if (!IsInventoryOpened && isGrounded)
            {
                Move(CurrentPlayerState.MovementDirection);
                if (_isServer)
                {
                    MovementContext.method_1(CurrentPlayerState.MovementDirection);
                }
            }

            Transform.position = CurrentPlayerState.Position;

            if (!Mathf.Approximately(MovementContext.Tilt, CurrentPlayerState.Tilt))
            {
                MovementContext.SetTilt(CurrentPlayerState.Tilt, true);
            }

            
            if (!Mathf.Approximately(ObservedOverlap, CurrentPlayerState.WeaponOverlap))
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

        public override void CreateNestedSource()
        {
            if (!FikaBackendUtils.IsHeadless)
            {
                base.CreateNestedSource();
            }
        }

        public override void CreateSpeechSource()
        {
            if (!FikaBackendUtils.IsHeadless)
            {
                base.CreateSpeechSource();
            }
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
            if (_cullingHandler != null)
            {
                _cullingHandler.DisableCullingOnDead();
            }
            if (!FikaBackendUtils.IsHeadless)
            {
                _observedCorpseCulling = new(this, Corpse);
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

                _lastWeaponId = packet.WeaponId;
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

        public void SetInventory(InventoryDescriptorClass inventoryDescriptor)
        {
            if (HandsController != null)
            {
                HandsController.FastForwardCurrentState();
            }

            Inventory inventory = new EFTInventoryClass()
            {
                Equipment = inventoryDescriptor
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
                _observedSlotViewHandlers.Add(handler);
            }

            if (PlayerBody.HaveHolster && PlayerBody.SlotViews.ContainsKey(EquipmentSlot.Holster))
            {
                Slot slot = Inventory.Equipment.GetSlot(EquipmentSlot.Holster);
                ObservedSlotViewHandler handler = new(slot, this, EquipmentSlot.Holster);
                _observedSlotViewHandlers.Add(handler);
            }

            if (HandsController != null && HandsController is CoopObservedFirearmController controller)
            {
                if (Inventory.Equipment.TryFindItem(controller.Weapon.Id, out Item item))
                {
                    if (item is not Weapon newWeapon)
                    {
                        FikaGlobals.LogError("SetInventory::HandsController item was not Weapon");
                        return;
                    }

                    IEnumerable<Slot> newSlots = newWeapon.AllSlots;
                    if (newSlots != null)
                    {
                        Dictionary<string, GClass764.GClass765> currentViews = [];
                        foreach (KeyValuePair<EFT.InventoryLogic.IContainer, GClass764.GClass765> kvp in controller.CCV.ContainerBones)
                        {
                            if (kvp.Key is Slot slot && slot.ContainedItem != null)
                            {
                                if (currentViews.ContainsKey(slot.FullId))
                                {
                                    FikaGlobals.LogError("RefreshSlotViews::CRITICAL ERROR DICTIONARY: " + slot.FullId);
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
                                    Transform transform = TransformHelperClass.FindTransformRecursive(controller.CCV.GameObject.transform, slot.ID, true);
                                    if (transform == null)
                                    {
                                        FikaGlobals.LogWarning($"RefreshSlotViews::Transform was missing: {slot.ID}, this is harmless");
                                        continue;
                                    }
                                    controller.CCV.AddBone(slot, transform);
                                    continue;
                                }
                                foreach (KeyValuePair<string, GClass764.GClass765> kvp in currentViews)
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

            _observedVaultingParameters.MaxWeightPointPosition = packet.VaultingPoint;
            _observedVaultingParameters.VaultingHeight = packet.VaultingHeight;
            _observedVaultingParameters.VaultingLength = packet.VaultingLength;
            _observedVaultingParameters.VaultingSpeed = packet.VaultingSpeed;
            _observedVaultingParameters.AbsoluteForwardVelocity = packet.AbsoluteForwardVelocity;
            _observedVaultingParameters.BehindObstacleRatio = packet.BehindObstacleHeight;

            MovementContext.PlayerAnimator.SetVaultingSpeed(packet.VaultingSpeed);
            MovementContext.PlayerAnimator.SetVaultingHeight(packet.VaultingHeight);
            MovementContext.PlayerAnimator.SetVaultingLength(packet.VaultingLength);
            MovementContext.PlayerAnimator.SetBehindObstacleRatio(packet.BehindObstacleHeight);
            MovementContext.PlayerAnimator.SetAbsoluteForwardVelocity(packet.AbsoluteForwardVelocity);

            MovementContext.PlayerAnimator.SetIsGrounded(true);
        }

        public void InitObservedPlayer()
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
                Profile.Info.GroupId = "Fika";
                Profile.Info.TeamId = "Fika";
                if (!FikaBackendUtils.IsHeadless)
                {
                    _waitForStartRoutine = StartCoroutine(CreateHealthBar());
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

                InitVaultingAudioControllers(_observedVaultingParameters);

                if (FikaPlugin.ShowNotifications.Value)
                {
                    NotificationManagerClass.DisplayMessageNotification(string.Format(LocaleUtils.GROUP_MEMBER_SPAWNED.Localized(),
                        ColorizeText(EColor.GREEN, Profile.Info.MainProfileNickname)),
                    EFT.Communications.ENotificationDurationType.Default, EFT.Communications.ENotificationIconType.Friend);
                }
            }
        }

        private IEnumerator CreateHealthBar()
        {
            IFikaGame fikaGame = Singleton<IFikaGame>.Instance;
            if (fikaGame == null)
            {
                yield break;
            }

            while (fikaGame.GameController.GameInstance.Status != GameStatus.Started)
            {
                yield return null;
            }

            _healthBar = FikaHealthBar.Create(this);

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
            if (HealthController == null || !HealthController.IsAlive)
            {
                return;
            }
            Physical.LateUpdate();
            ObservedVisualPass(Time.deltaTime, 3);
            PropUpdate();
            _observedCorpseCulling?.ManualUpdate();
            _armsupdated = false;
            _bodyupdated = false;
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
            method_13(deltaTime);

            if (HealthController.IsAlive)
            {
                if (Time.frameCount % 2 == _frameSkip)
                {
                    UpdateTriggerColliderSearcher(deltaTime, _cullingHandler.IsCloseToMyPlayerCamera);
                }
                _cullingHandler.ManualUpdate(deltaTime);
            }
        }

        public override void InitAudioController()
        {
            if (!FikaBackendUtils.IsHeadless)
            {
                base.InitAudioController();
                Singleton<BetterAudio>.Instance.ProtagonistHearingChanged += UpdateSoundRolloff;
            }
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
            foreach (ObservedSlotViewHandler slotViewHandler in _observedSlotViewHandlers)
            {
                slotViewHandler.Dispose();
            }
            _observedSlotViewHandlers.Clear();
            _observedCorpseCulling?.Dispose();
            if (HealthController.IsAlive)
            {
                if (!Singleton<IFikaNetworkManager>.Instance.ObservedCoopPlayers.Remove(this) && !Profile.Nickname.StartsWith("headless_"))
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
            private readonly Slot _slot;
            private readonly ObservedCoopPlayer _observedPlayer;
            private readonly EquipmentSlot _slotType;

            public ObservedSlotViewHandler(Slot itemSlot, ObservedCoopPlayer player, EquipmentSlot equipmentType)
            {
                _slot = itemSlot;
                _observedPlayer = player;
                _slotType = equipmentType;

                itemSlot.OnAddOrRemoveItem += HandleItemMove;
            }

            public void Dispose()
            {
                _slot.OnAddOrRemoveItem -= HandleItemMove;
            }

            private void HandleItemMove(Item item)
            {
                Transform slotBone = _observedPlayer.PlayerBody.GetSlotBone(_slotType);
                Transform alternativeHolsterBone = _observedPlayer.PlayerBody.GetAlternativeHolsterBone(_slotType);
                PlayerBody.GClass2182 newSlotView = new(_observedPlayer.PlayerBody, _slot, slotBone, _slotType,
                        _observedPlayer.Inventory.Equipment.GetSlot(EquipmentSlot.Backpack), alternativeHolsterBone, false);
                PlayerBody.GClass2182 oldSlotView = _observedPlayer.PlayerBody.SlotViews.AddOrReplace(_slotType, newSlotView);
                if (oldSlotView != null)
                {
                    ClearSlotView(oldSlotView);
                    oldSlotView.Dispose();
                }
                _observedPlayer.PlayerBody.ValidateHoodedDress(_slotType);
                GlobalEventHandlerClass.Instance.CreateCommonEvent<GClass3423>().Invoke(_observedPlayer.ProfileId);
                Dispose();
            }

            private void ClearSlotView(PlayerBody.GClass2182 oldSlotView)
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
                        GStruct57 bodyRenderer = dresses[j].GetBodyRenderer();
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
            CreateHandsControllerHandler handler = new((item != null) ? method_137(item) : null);

            handler.SetInHandsOperation?.Confirm(true);

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
            OnSetInHands(new(item, CommandStatus.Succeed, InventoryController));
            _shouldCullController = _handsController is EmptyHandsController || _handsController is KnifeController || _handsController is UsableItemController;
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
            GStruct461<Item> result = FindItemById(itemId, false, false);
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

            GStruct461<Item> result = FindItemById(itemId, false, false);
            if (!result.Succeeded)
            {
                FikaPlugin.Instance.FikaLogger.LogError(result.Error);
                return;
            }
            handler.Item = result.Value;
            if (handler.Item is ThrowWeapItemClass)
            {
                CreateHandsController(handler.ReturnController, handler.Item);
            }
            else
            {
                FikaPlugin.Instance.FikaLogger.LogError($"CreateGrenadeController: Item was not of type GrenadeClass, was {handler.Item.GetType()}!");
            }
        }

        private void CreateMedsController(string itemId, GStruct375<EBodyPart> bodyParts, float amount, int animationVariant)
        {
            GStruct461<Item> result = FindItemById(itemId, false, false);
            if (!result.Succeeded)
            {
                FikaPlugin.Instance.FikaLogger.LogError(result.Error);
                return;
            }
            CreateMedsControllerHandler handler = new(this, result.Value, bodyParts, amount, animationVariant);
            CreateHandsController(handler.ReturnController, handler.Item);
        }

        private void CreateKnifeController(string itemId)
        {
            CreateKnifeControllerHandler handler = new(this);
            GStruct461<Item> result = FindItemById(itemId, false, false);
            if (!result.Succeeded)
            {
                FikaPlugin.Instance.FikaLogger.LogError(result.Error);
                return;
            }
            handler.Knife = result.Value.GetItemComponent<KnifeComponent>();
            if (handler.Knife != null)
            {
                CreateHandsController(handler.ReturnController, handler.Knife.Item);
            }
            else
            {
                FikaPlugin.Instance.FikaLogger.LogError($"CreateKnifeController: Item did not contain a KnifeComponent, was of type {handler.Knife.GetType()}!");
            }
        }

        private void CreateQuickGrenadeController(string itemId)
        {
            CreateQuickGrenadeControllerHandler handler = new(this);
            GStruct461<Item> result = FindItemById(itemId, false, false);
            if (!result.Succeeded)
            {
                FikaPlugin.Instance.FikaLogger.LogError(result.Error);
                return;
            }
            handler.tem = result.Value;
            if (handler.tem is ThrowWeapItemClass)
            {
                CreateHandsController(handler.ReturnController, handler.tem);
            }
            else
            {
                FikaPlugin.Instance.FikaLogger.LogError($"CreateQuickGrenadeController: Item was not of type GrenadeClass, was {handler.tem.GetType()}!");
            }
        }

        private void CreateQuickKnifeController(string itemId)
        {
            CreateQuickKnifeControllerHandler handler = new(this);
            GStruct461<Item> result = FindItemById(itemId, false, false);
            if (!result.Succeeded)
            {
                FikaPlugin.Instance.FikaLogger.LogError(result.Error);
                return;
            }
            handler.Knife = result.Value.GetItemComponent<KnifeComponent>();
            if (handler.Knife != null)
            {
                CreateHandsController(handler.ReturnController, handler.Knife.Item);
            }
            else
            {
                FikaPlugin.Instance.FikaLogger.LogError($"CreateQuickKnifeController: Item did not contain a KnifeComponent, was of type {handler.Knife.GetType()}!");
            }
        }

        private void CreateUsableItemController(string itemId)
        {
            GStruct461<Item> result = FindItemById(itemId, false, false);
            if (!result.Succeeded)
            {
                FikaPlugin.Instance.FikaLogger.LogError(result.Error);
                return;
            }
            CreateUsableItemControllerHandler handler = new(this, result.Value);
            CreateHandsController(handler.ReturnController, handler.Item);
        }

        private void CreateQuickUseItemController(string itemId)
        {
            GStruct461<Item> result = FindItemById(itemId, false, false);
            if (!result.Succeeded)
            {
                FikaPlugin.Instance.FikaLogger.LogError(result.Error);
                return;
            }
            CreateQuickUseItemControllerHandler handler = new(this, result.Value);
            CreateHandsController(handler.ReturnController, handler.Item);
        }

        public void SetAggressorData(string killerId, EBodyPart bodyPart, string weaponId)
        {
            Player killer = Singleton<GameWorld>.Instance.GetEverExistedPlayerByID(killerId);
            if (killer != null)
            {
                LastAggressor = killer;
            }
            LastBodyPart = bodyPart;
            _lastWeaponId = weaponId;

            if (LastDamageInfo.Weapon is null && !string.IsNullOrEmpty(_lastWeaponId))
            {
                FindKillerWeapon();
            }
        }

        private void ObservedVisualPass(float deltaTime, int ikUpdateInterval)
        {
            if (CustomAnimationsAreProcessing || !_cullingHandler.IsVisible || !HealthController.IsAlive)
            {
                return;
            }

            _lastDistance = CameraClass.Instance.Distance(Transform.position);
            bool isVisibleOrClose = IsVisible && _lastDistance <= EFTHardSettings.Instance.CULL_GROUNDER;

            if (_armsupdated && isVisibleOrClose && !UsedSimplifiedSkeleton)
            {
                ProceduralWeaponAnimation.ProcessEffectors(deltaTime, 2, Motion, Velocity);
                PlayerBones.Offset = ProceduralWeaponAnimation.HandsContainer.WeaponRootAnim.localPosition;
                PlayerBones.DeltaRotation = ProceduralWeaponAnimation.HandsContainer.WeaponRootAnim.localRotation;
            }

            if (isVisibleOrClose && !UsedSimplifiedSkeleton)
            {
                RestoreIKPos();
                ObservedFBBIKUpdate(_lastDistance, ikUpdateInterval);
                MouseLook(false);
                float num2 = 1f;
                float num4 = method_25(PlayerAnimator.LEFT_STANCE_CURVE);
                ProceduralWeaponAnimation.GetLeftStanceCurrentCurveValue(num4);
                _rightHand = 1f - method_25(PlayerAnimator.RIGHT_HAND_WEIGHT) * num2;
                _leftHand = 1f - method_25(PlayerAnimator.LEFT_HAND_WEIGHT) * num2;
                ThirdPersonWeaponRootAuthority = MovementContext.IsInMountedState ? 0f : (method_25(PlayerAnimator.WEAPON_ROOT_3RD) * num2);
                method_23(_lastDistance);
                if (_armsupdated)
                {
                    float num5 = ThirdPersonWeaponRootAuthority;
                    if (MovementContext.StationaryWeapon != null)
                    {
                        num5 = 0f;
                    }
                    ProceduralWeaponAnimation.GetLeftStanceCurrentCurveValue(num4);
                    PlayerBones.ShiftWeaponRoot(deltaTime, EPointOfView.ThirdPerson, num5);
                }
                PlayerBones.RotateHead(0f, ProceduralWeaponAnimation.GetHeadRotation(),
                    MovementContext.LeftStanceEnabled && HasFirearmInHands(), num4,
                    ProceduralWeaponAnimation.IsAiming);
                HandPosers[0].weight = _leftHand;
                _observedLimbs[0].solver.IKRotationWeight = _observedLimbs[0].solver.IKPositionWeight = _leftHand;
                _observedLimbs[1].solver.IKRotationWeight = _observedLimbs[1].solver.IKPositionWeight = _rightHand;
                method_20(_lastDistance);
                method_24(num2);
                method_19(_lastDistance);
                if (_rightHand < 1f)
                {
                    PlayerBones.Kinematics(_observedMarkers[1], _rightHand);
                }
                float num6 = method_25(PlayerAnimator.AIMING_LAYER_CURVE);
                MovementContext.PlayerAnimator.Animator.SetLayerWeight(6, 1f - num6);
                _prevHeight = Transform.position.y;
            }
            else
            {
                if (!Mathf.Approximately(PlayerBones.AnimatedTransform.localPosition.y, 0f))
                {
                    PlayerBones.AnimatedTransform.localPosition = new Vector3(PlayerBones.AnimatedTransform.localPosition.x, 0f, PlayerBones.AnimatedTransform.localPosition.z);
                }
                MouseLook(false);
                Transform child = PlayerBones.Weapon_Root_Anim.GetChild(0);
                child.localPosition = Vector3.zero;
                child.localRotation = Quaternion.identity;
            }
            if (_lastDistance > EFTHardSettings.Instance.AnimatorCullDistance)
            {
                BodyAnimatorCommon.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                ArmsAnimatorCommon.cullingMode = _shouldCullController ? AnimatorCullingMode.AlwaysAnimate : AnimatorCullingMode.CullUpdateTransforms;
            }
            else
            {
                BodyAnimatorCommon.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                ArmsAnimatorCommon.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
            if (_armsupdated)
            {
                ProceduralWeaponAnimation.LateTransformations(deltaTime);
                if (HandsController != null)
                {
                    HandsController.ManualLateUpdate(deltaTime);
                }
            }
        }

        private void ObservedFBBIKUpdate(float distance, int ikUpdateInterval)
        {
            _fbbik.solver.iterations = (int)Mathf.Clamp(15f / distance, 0f, 2f);
            if (!_fbbik.solver.Quick && Time.time > TurnOffFbbikAt)
            {
                _fbbik.solver.Quick = true;
            }
            if (!_fbbik.solver.Quick || Time.frameCount % ikUpdateInterval == 0)
            {
                _fbbik.solver.Update();
            }
        }

        private class RemoveHandsControllerHandler(ObservedCoopPlayer coopPlayer, Callback callback)
        {
            private readonly ObservedCoopPlayer _coopPlayer = coopPlayer;
            private readonly Callback _callback = callback;

            public void Handle(Result<GInterface160> result)
            {
                if (_coopPlayer._removeFromHandsCallback == _callback)
                {
                    _coopPlayer._removeFromHandsCallback = null;
                }
                _callback.Invoke(result);
            }
        }

        private class CreateHandsControllerHandler(Class1218 setInHandsOperation)
        {
            public readonly Class1218 SetInHandsOperation = setInHandsOperation;

            internal void DisposeHandler()
            {
                Class1218 handler = SetInHandsOperation;
                if (handler == null)
                {
                    return;
                }
                handler.Dispose();
            }
        }

        private class CreateFirearmControllerHandler(ObservedCoopPlayer coopPlayer)
        {
            private readonly ObservedCoopPlayer _coopPlayer = coopPlayer;
            public Item item;

            internal AbstractHandsController ReturnController()
            {
                return CoopObservedFirearmController.Create(_coopPlayer, (Weapon)item);
            }
        }

        private class CreateGrenadeControllerHandler(ObservedCoopPlayer coopPlayer)
        {
            private readonly ObservedCoopPlayer coopPlayer = coopPlayer;
            public Item Item;

            internal AbstractHandsController ReturnController()
            {
                return CoopObservedGrenadeController.Create(coopPlayer, (ThrowWeapItemClass)Item);
            }
        }

        private class CreateMedsControllerHandler(ObservedCoopPlayer coopPlayer, Item item, GStruct375<EBodyPart> bodyParts, float amount, int animationVariant)
        {
            private readonly ObservedCoopPlayer _coopPlayer = coopPlayer;
            public readonly Item Item = item;
            private readonly GStruct375<EBodyPart> _bodyParts = bodyParts;
            private readonly float _amount = amount;
            private readonly int _animationVariant = animationVariant;

            internal AbstractHandsController ReturnController()
            {
                return CoopObservedMedsController.Create(_coopPlayer, Item, _bodyParts, _amount, _animationVariant);
            }
        }

        private class CreateKnifeControllerHandler(ObservedCoopPlayer coopPlayer)
        {
            private readonly ObservedCoopPlayer _coopPlayer = coopPlayer;
            public KnifeComponent Knife;

            internal AbstractHandsController ReturnController()
            {
                return CoopObservedKnifeController.Create(_coopPlayer, Knife);
            }
        }

        private class CreateQuickGrenadeControllerHandler(ObservedCoopPlayer coopPlayer)
        {
            private readonly ObservedCoopPlayer _coopPlayer = coopPlayer;
            public Item tem;

            internal AbstractHandsController ReturnController()
            {
                return CoopObservedQuickGrenadeController.Create(_coopPlayer, (ThrowWeapItemClass)tem);
            }
        }

        private class CreateQuickKnifeControllerHandler(ObservedCoopPlayer coopPlayer)
        {
            private readonly ObservedCoopPlayer _coopPlayer = coopPlayer;
            public KnifeComponent Knife;

            internal AbstractHandsController ReturnController()
            {
                return QuickKnifeKickController.smethod_9<QuickKnifeKickController>(_coopPlayer, Knife);
            }
        }

        private class CreateUsableItemControllerHandler(ObservedCoopPlayer coopPlayer, Item item)
        {
            private readonly ObservedCoopPlayer _coopPlayer = coopPlayer;
            public readonly Item Item = item;

            internal AbstractHandsController ReturnController()
            {
                return UsableItemController.smethod_6<UsableItemController>(_coopPlayer, Item);
            }
        }

        private class CreateQuickUseItemControllerHandler(ObservedCoopPlayer coopPlayer, Item item)
        {
            private readonly ObservedCoopPlayer _coopPlayer = coopPlayer;
            public readonly Item Item = item;

            internal AbstractHandsController ReturnController()
            {
                return QuickUseItemController.smethod_6<QuickUseItemController>(_coopPlayer, Item);
            }
        }
    }
}

#endregion