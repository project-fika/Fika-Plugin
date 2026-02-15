// © 2026 Lacyway All Rights Reserved

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Audio.SpatialSystem;
using Comfort.Common;
using Dissonance;
using EFT;
using EFT.AssetsManager;
using EFT.Ballistics;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.Vaulting;
using Fika.Core.Main.Components;
using Fika.Core.Main.Factories;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.ObservedClasses;
using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.ObservedClasses.Snapshotting;
using Fika.Core.Main.PacketHandlers;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.Communication;
using Fika.Core.Networking.Packets.Player.Common;
using Fika.Core.Networking.Packets.Player.Common.SubPackets;
using HarmonyLib;
using JsonType;
using RootMotion.FinalIK;
using static Fika.Core.UI.FikaUIGlobals;

namespace Fika.Core.Main.Players;

/// <summary>
/// Observed players are any other players in the world for a client, including bots. <br/>
/// Bots are handled by the server, and other clients send their own data which the server replicates to other clients. <br/>
/// As a host all <see cref="ObservedPlayer"/>s are only other clients.
/// </summary>
public class ObservedPlayer : FikaPlayer
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

    public ObservedHealthController NetworkHealthController
    {
        get
        {
            return HealthController as ObservedHealthController;
        }
    }

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
            var pointOfViewChanged = PointOfViewChanged;
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
            var weaponAnimationType = GetWeaponAnimationType(_handsController);
            MovementContext.PlayerAnimatorSetWeaponId(weaponAnimationType);
        }
    }

    public override Ray InteractionRay
    {
        get
        {
            var vector = HandsRotation * Vector3.forward;
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

    public override bool IsVisible
    {
        get
        {
            if (FikaBackendUtils.IsHeadless)
            {
                return true;
            }
            return _followerCullingObject != null && _followerCullingObject.IsVisible;
        }

        set
        {

        }
    }

    public GClass782 ObservedCharacterController
    {
        get
        {
            return MovementContext.GClass782_0;
        }
    }

    public float TurnOffFbbikAt;

    internal ObservedState CurrentPlayerState;

    private float _lastDistance;
    private LocalPlayerCullingHandlerClass _cullingHandler;
    private float _rightHand;
    private float _leftHand;
    private LimbIK[] _observedLimbs;
    private Transform[] _observedMarkers;
    private bool _shouldCullController;
    private readonly List<ObservedSlotViewHandler> _observedSlotViewHandlers = [];
    private ObservedCorpseCulling _observedCorpseCulling;
    private bool _compassLoaded;
    private FollowerCullingObject _followerCullingObject;
    private readonly ObservedVaultingParametersClass _observedVaultingParameters = new();
    private bool _leftStancedDisabled;
    private FikaHealthBar _healthBar;
    private Coroutine _waitForStartRoutine;
    private bool _isServer;
    private VoiceBroadcastTrigger _voiceBroadcastTrigger;
    private SoundSettingsControllerClass _soundSettings;
    private bool _voipAssigned;
    private int _frameSkip;
    #endregion

    public static async Task<ObservedPlayer> CreateObservedPlayer(GameWorld gameWorld, int playerId, Vector3 position, Quaternion rotation, string layerName,
        string prefix, EPointOfView pointOfView, Profile profile, byte[] healthBytes, bool aiControl,
        EUpdateQueue updateQueue, EUpdateMode armsUpdateMode, EUpdateMode bodyUpdateMode,
        CharacterControllerSpawner.Mode characterControllerMode, Func<float> getSensitivity, Func<float> getAimingSensitivity,
        IViewFilter filter, MongoID firstId, ushort firstOperationId, bool isZombie)
    {
        var useSimpleAnimator = isZombie;
#if DEBUG
        if (useSimpleAnimator)
        {
            FikaGlobals.LogWarning("Using SimpleAnimator!");
        }
#endif
        var resourceKey = useSimpleAnimator ? ResourceKeyManagerAbstractClass.ZOMBIE_BUNDLE_NAME : ResourceKeyManagerAbstractClass.PLAYER_BUNDLE_NAME;
        var player = Create<ObservedPlayer>(gameWorld, resourceKey, playerId, position, updateQueue,
            armsUpdateMode, bodyUpdateMode, characterControllerMode, getSensitivity, getAimingSensitivity, prefix, aiControl, useSimpleAnimator);

        player.IsYourPlayer = false;
        player.IsObservedAI = aiControl;
        player.CommonPacket = new()
        {
            NetId = playerId
        };

        ObservedInventoryController inventoryController = new(player, profile, true, firstId, firstOperationId, aiControl);
        ObservedHealthController healthController = new(healthBytes, player, inventoryController, profile.Skills);

        ObservedStatisticsManager statisticsManager = new();
        ObservedQuestController observedQuestController = null;
        GClass3618 dialogueController = null;
        if (!aiControl)
        {
            observedQuestController = new(profile, inventoryController, inventoryController.PlayerSearchController, null);
            observedQuestController.Init();
            observedQuestController.Run();

            dialogueController = new(profile, observedQuestController, inventoryController);
        }

        player.VoipState = (!FikaBackendUtils.IsHeadless && !aiControl && Singleton<IFikaNetworkManager>.Instance.AllowVOIP)
            ? EVoipState.Available : EVoipState.NotAvailable;

        await player.Init(rotation, layerName, pointOfView, profile, inventoryController, healthController,
            statisticsManager, observedQuestController, null,
            null, dialogueController, filter, player.VoipState, aiControl, false);

        player.DisposeObservers();

        player.Pedometer.Stop();
        player._handsController = EmptyHandsController.smethod_6<EmptyHandsController>(player);
        player._handsController.Spawn(1f, FikaGlobals.EmptyAction);

        player.AIData = new PlayerAIDataClass(null, player);

        var observedTraverse = Traverse.Create(player);
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
            var services = Traverse.Create(player).Field<HashSet<ETraderServiceType>>("hashSet_0").Value;
            foreach (var etraderServiceType in Singleton<BackendConfigSettingsClass>.Instance.ServicesData.Keys)
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

        if (ObservedPlayerControllerClass.Int_1 == 0)
        {
            ObservedPlayerControllerClass.Int_1 = 1;
            player._frameSkip = 1;
        }
        else
        {
            ObservedPlayerControllerClass.Int_1 = 0;
            player._frameSkip = 0;
        }

        CameraClass.Instance.FoVUpdateAction -= player.OnFovUpdatedEvent;

        if (!FikaBackendUtils.IsHeadless)
        {
            player._followerCullingObject = player.gameObject.AddComponent<FollowerCullingObject>();
            player._followerCullingObject.enabled = true;
            player._followerCullingObject.CullByDistanceOnly = false;
            player._followerCullingObject.Init(player.GetCullingTransform);
            player._followerCullingObject.SetParams(EFTHardSettings.Instance.CULLING_PLAYER_SPHERE_RADIUS,
                EFTHardSettings.Instance.CULLING_PLAYER_SPHERE_SHIFT, EFTHardSettings.Instance.CULLING_PLAYER_DISTANCE);
        }

        player.SubscribeToArmorChangeEvent();
        player.RecalculateEquippedArmorComponents(null);

        return player;
    }

    private Transform GetCullingTransform()
    {
        return PlayerBones.BodyTransform.Original;
    }

    /// <summary>
    /// These are redundant on observed players
    /// </summary>
    private void DisposeObservers()
    {
        NightVisionObserver.Dispose();
        ThermalVisionObserver.Dispose();
        FaceCoverObserver.Dispose();
        FaceCoverObserver.Dispose();
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
                StartCoroutine(SourceBindingCreated());
                if (VoipAudioSource != null)
                {

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

    private IEnumerator SourceBindingCreated()
    {
        if (_voipAssigned)
        {
            yield break;
        }

        if (VoipAudioSource == null)
        {
            var attempts = 0;
            var waitForSeconds = new WaitForSeconds(1);

            while (VoipAudioSource == null)
            {
                FikaGlobals.LogInfo($"VoipAudioSource is null, waiting 1 second... [Attempt {attempts + 1}]");

                if (attempts >= 5)
                {
                    FikaGlobals.LogError("VoipAudioSource was null after 5 attempts! Cancelling.");
                    yield break;
                }

                attempts++;
                yield return waitForSeconds;
            }
        }

        VoipEftSource = MonoBehaviourSingleton<BetterAudio>.Instance.CreateBetterSource<SimpleSource>(
            VoipAudioSource, BetterAudio.AudioSourceGroupType.Voip, true, true);
        if (VoipEftSource == null)
        {
            FikaGlobals.LogError($"Could not initialize VoipEftSource for {Profile.Nickname}");
            yield break;
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
        CompositeDisposable.BindState(_soundSettings.VoiceChatVolume, ChangeVoipDeviceSensitivity);
    }

    private void ChangeVoipDeviceSensitivity(int value)
    {
        var num = (float)value / 100f;
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
        (var hit, var surfaceSound) = method_75();
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
        var player = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(DamageInfo.Player.iPlayer.ProfileId);
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
            var flag = DamageInfo.DidBodyDamage / HealthController.GetBodyPartHealth(bodyPart, false).Maximum >= 0.6f && HealthController.FindExistingEffect<GInterface341>(bodyPart) != null;
            player.StatisticsManager.OnEnemyDamage(DamageInfo, bodyPart, ProfileId, Side, Profile.Info.Settings.Role,
                GroupId, HealthController.GetBodyPartHealth(EBodyPart.Common, false).Maximum, flag,
                Vector3.Distance(player.Transform.position, Transform.position), CurrentHour,
                Inventory.EquippedInSlotsTemplateIds, HealthController.BodyPartEffects, TriggerZones);
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

        CommonPacket.Type = ECommonSubPacketType.Damage;
        CommonPacket.SubPacket = DamagePacket.FromValue(NetId, DamageInfo, bodyPartType, colliderType);
        Singleton<IFikaNetworkManager>.Instance.SendNetReusable(ref CommonPacket, DeliveryMethod.ReliableOrdered, true);
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

        CommonPacket.Type = ECommonSubPacketType.Damage;
        CommonPacket.SubPacket = DamagePacket.FromValue(NetId, DamageInfo, bodyPartType, colliderType, armorPlateCollider);
        Singleton<IFikaNetworkManager>.Instance.SendNetReusable(ref CommonPacket, DeliveryMethod.ReliableOrdered, true);

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
        var flag = damageInfo.DeflectedBy != null;
        var damage = damageInfo.Damage;
        var list = ProceedDamageThroughArmor(ref damageInfo, colliderType, armorPlateCollider, true);
        var materialType = flag ? MaterialType.HelmetRicochet : ((list == null || list.Count < 1)
            ? MaterialType.Body : list[0].Material);
        ShotInfoClass hitInfo = new()
        {
            PoV = PointOfView,
            Penetrated = damageInfo.Penetrated,
            Material = materialType
        };
        var num = damage - damageInfo.Damage;
        if (num > 0)
        {
            damageInfo.DidArmorDamage = num;
        }
        damageInfo.DidBodyDamage = damageInfo.Damage;
        ReceiveDamage(damageInfo.Damage, bodyPartType, damageInfo.DamageType, num, hitInfo.Material);

        CommonPacket.Type = ECommonSubPacketType.Damage;
        CommonPacket.SubPacket = DamagePacket.FromValue(NetId, damageInfo, bodyPartType, colliderType, armorPlateCollider, absorbed: num);
        Singleton<IFikaNetworkManager>.Instance.SendNetReusable(ref CommonPacket, DeliveryMethod.ReliableOrdered, true);

        // Run this to get weapon skill
        ManageAggressor(damageInfo, bodyPartType, colliderType);

        return hitInfo;
    }

    public override void OnItemAddedOrRemoved(Item item, ItemAddress location, bool added)
    {
        // Do nothing
    }

    public override void ApplyExplosionDamageToArmor(Dictionary<ExplosiveHitArmorColliderStruct, float> armorDamage, DamageInfoStruct DamageInfo)
    {
        if (_isServer)
        {
            foreach (var armorComponent in _preAllocatedArmorComponents)
            {
                var num = 0f;
                foreach (var keyValuePair in armorDamage)
                {
                    if (armorComponent.ShotMatches(keyValuePair.Key.BodyPartColliderType, keyValuePair.Key.ArmorPlateCollider))
                    {
                        num += keyValuePair.Value;
                    }
                }
                if (num > 0f)
                {
                    num = armorComponent.ApplyExplosionDurabilityDamage(num, DamageInfo, _preAllocatedArmorComponents);
                    method_96(num, armorComponent);
                    OnArmorPointsChanged(armorComponent);
                }
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

        var flag = damageInfo.DeflectedBy != null;
        var damage = damageInfo.Damage;
        var list = ProceedDamageThroughArmor(ref damageInfo, colliderType, armorPlateCollider, true);
        var materialType = flag ? MaterialType.HelmetRicochet : ((list == null || list.Count < 1)
            ? MaterialType.Body : list[0].Material);
        ShotInfoClass hitInfo = new()
        {
            PoV = PointOfView,
            Penetrated = damageInfo.Penetrated,
            Material = materialType
        };
        var num = damage - damageInfo.Damage;
        if (num > 0)
        {
            damageInfo.DidArmorDamage = num;
        }
        damageInfo.DidBodyDamage = damageInfo.Damage;
        ReceiveDamage(damageInfo.Damage, bodyPartType, damageInfo.DamageType, num, hitInfo.Material);

        CommonPacket.Type = ECommonSubPacketType.Damage;
        CommonPacket.SubPacket = DamagePacket.FromValue(NetId, damageInfo, bodyPartType, colliderType, armorPlateCollider, absorbed: num);
        Singleton<IFikaNetworkManager>.Instance.SendNetReusable(ref CommonPacket, DeliveryMethod.ReliableOrdered, true);

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
                    && PlayerBones.BodyPartCollidersDictionary.TryGetValue(CorpseSyncPacket.BodyPartColliderType, out var bodyPartCollider))
            {
                Corpse.Ragdoll.ApplyImpulse(bodyPartCollider.Collider, CorpseSyncPacket.Direction, CorpseSyncPacket.Point, CorpseSyncPacket.Force);
            }
        }
    }

    public override void CreateMovementContext()
    {
        var movement_MASK = EFTHardSettings.Instance.MOVEMENT_MASK;
        MovementContext = ObservedMovementContext.Create(this, GetBodyAnimatorCommon, GetCharacterControllerCommon, movement_MASK);
    }

    public override void OnHealthEffectAdded(IEffect effect)
    {
        // Check for GClass increments
        if (effect is GInterface342 fracture && !fracture.WasPaused && FractureSound != null && Singleton<BetterAudio>.Instantiated)
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
    public override void Proceed(bool withNetwork, Callback<GInterface198> callback, bool scheduled = true)
    {
        Func<EmptyHandsController> func = new(ProceedEmptyHandsController);
        new Process<EmptyHandsController, GInterface198>(this, func, null, false)
            .method_0(null, callback, scheduled);
    }

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

    public override void Proceed(ThrowWeapItemClass throwWeap, Callback<GInterface206> callback, bool scheduled = true)
    {
        HandsControllerFactory factory = new(this, throwWeap);
        Func<QuickGrenadeThrowHandsController> func = new(factory.CreateObservedQuickGrenadeController);
        new Process<QuickGrenadeThrowHandsController, GInterface206>(this, func, throwWeap, false)
            .method_0(null, callback, scheduled);
    }

    public override void Proceed(Weapon weapon, Callback<IFirearmHandsController> callback, bool scheduled = true)
    {
        HandsControllerFactory factory = new(this, weapon);
        Func<FirearmController> func = new(factory.CreateObservedFirearmController);
        new Process<FirearmController, IFirearmHandsController>(this, func, factory.Item, true)
            .method_0(null, callback, scheduled);
    }

    public override void Proceed(MedsItemClass meds, GStruct382<EBodyPart> bodyParts, Callback<GInterface203> callback, int animationVariant, bool scheduled = true)
    {
        HandsControllerFactory factory = new(this)
        {
            MedsItem = meds,
            BodyParts = bodyParts,
            AnimationVariant = animationVariant
        };
        Func<MedsController> func = new(factory.CreateObservedMedsController);
        new Process<MedsController, GInterface203>(this, func, meds, false)
            .method_0(null, callback, scheduled);
    }

    public override void Proceed(FoodDrinkItemClass foodDrink, float amount, Callback<GInterface203> callback, int animationVariant, bool scheduled = true)
    {
        HandsControllerFactory factory = new(this)
        {
            FoodItem = foodDrink,
            Amount = amount,
            AnimationVariant = animationVariant
        };
        Func<MedsController> func = new(factory.CreateObservedMedsController);
        new Process<MedsController, GInterface203>(this, func, foodDrink, false)
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

        (var hit, var surfaceSound) = method_75();
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
        if (!_cullingHandler.IsVisible)
        {
            Position = CurrentPlayerState.Position;
            Rotation = CurrentPlayerState.Rotation;
            ObservedCharacterController.Vector3_0 = CurrentPlayerState.Velocity;

            return;
        }

        Rotation = CurrentPlayerState.Rotation;

        HeadRotation = CurrentPlayerState.HeadRotation;
        ProceduralWeaponAnimation.SetHeadRotation(CurrentPlayerState.HeadRotation);

        var newState = CurrentPlayerState.State;

        if (newState == EPlayerState.Jump)
        {
            MovementContext.PlayerAnimatorEnableJump(true);
            if (_isServer)
            {
                MovementContext.method_2(1f);
            }
        }

        var isGrounded = CurrentPlayerState.IsGrounded;
        MovementContext.IsGrounded = isGrounded;

        if (isGrounded)
        {
            MovementContext.PlayerAnimatorEnableJump(false);
            MovementContext.PlayerAnimatorEnableLanding(true);
        }

        MovementContext.PlayerAnimatorEnableInert(CurrentPlayerState.IsMoving);
        MovementContext.MovementDirection = CurrentPlayerState.MovementDirection;
        if (_isServer && CurrentPlayerState.IsMoving)
        {
            MovementContext.method_1(CurrentPlayerState.MovementDirection);
        }

        Physical.SerializationStruct = CurrentPlayerState.Stamina;

        if (MovementContext.Step != CurrentPlayerState.Step)
        {
            CurrentManagedState.SetStep(CurrentPlayerState.Step);
        }

        if (Physical.Sprinting != CurrentPlayerState.IsSprinting)
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

        // hacky way to set velocity
        ObservedCharacterController.Vector3_0 = CurrentPlayerState.Velocity;

        CurrentPlayerState.ShouldUpdate = false;
    }

    public override void InteractionRaycast()
    {
        if (_playerLookRaycastTransform == null || !HealthController.IsAlive)
        {
            return;
        }

        InteractableObjectIsProxy = false;
        var interactionRay = InteractionRay;
        Boolean_0 = false;
        var gameObject = GameWorld.FindInteractable(interactionRay, out _);
        if (gameObject != null)
        {
            var player = gameObject.GetComponent<Player>();
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
            var observedCorpse = CreateCorpse<ObservedCorpse>(Velocity);
            observedCorpse.IsZombieCorpse = UsedSimplifiedSkeleton;
            observedCorpse.SetSpecificSettings(PlayerBones.RightPalm);
            Singleton<GameWorld>.Instance.ObservedPlayersCorpses.Add(NetId, observedCorpse);
            return observedCorpse;
        }

        var corpse = CreateCorpse<Corpse>(Velocity);
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

        if (FikaPlugin.Instance.Settings.ShowNotifications.Value)
        {
            if (!IsObservedAI)
            {
                var nickname = !string.IsNullOrEmpty(Profile.Info.MainProfileNickname) ? Profile.Info.MainProfileNickname : Profile.Nickname;
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
            if (LocaleUtils.IsBoss(Profile.Info.Settings.Role, out var name) && IsObservedAI && LastAggressor != null)
            {
                if (LastAggressor is FikaPlayer aggressor)
                {
                    var aggressorNickname = !string.IsNullOrEmpty(LastAggressor.Profile.Info.MainProfileNickname) ? LastAggressor.Profile.Info.MainProfileNickname : LastAggressor.Profile.Nickname;
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
        Singleton<IFikaNetworkManager>.Instance.ObservedPlayers.Remove(this);
    }

    public override void vmethod_3(TransitControllerAbstractClass controller, int transitPointId, string keyId, EDateTime time)
    {
        // Do nothing
    }

    public override void HandleDamagePacket(DamagePacket packet)
    {
        DamageInfoStruct damageInfo = new()
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
            ArmorDamage = packet.ArmorDamage
        };

        if (packet.SourceId.HasValue)
        {
            damageInfo.SourceId = packet.SourceId.Value;
        }

        if (packet.ProfileId.HasValue)
        {
            var player = Singleton<GameWorld>.Instance.GetAlivePlayerBridgeByProfileID(packet.ProfileId.Value);

            if (player != null)
            {
                damageInfo.Player = player;
                LastAggressor = player.iPlayer;
            }

            _lastWeaponId = packet.WeaponId;
        }

        ShotReactions(damageInfo, packet.BodyPartType);
        ReceiveDamage(damageInfo.Damage, packet.BodyPartType, damageInfo.DamageType, packet.Absorbed, packet.Material);

        LastDamageInfo = damageInfo;
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
            var mainPlayer = (FikaPlayer)Singleton<GameWorld>.Instance.MainPlayer;
            if (mainPlayer == null)
            {
                return;
            }

            if (!mainPlayer.HealthController.IsAlive)
            {
                return;
            }

            var role = Profile.Info.Settings.Role;
            var countAsBoss = role.CountAsBossForStatistics() && !(role is WildSpawnType.pmcUSEC or WildSpawnType.pmcBEAR);
            var experience = Profile.Info.Settings.Experience;
            var sessionCounters = mainPlayer.Profile.EftStats.SessionCounters;
            HandleSharedExperience(countAsBoss, experience, sessionCounters);

            if (FikaPlugin.Instance.SharedQuestProgression && FikaPlugin.Instance.Settings.EasyKillConditions.Value)
            {
#if DEBUG
                FikaGlobals.LogInfo("Handling teammate kill from teammate: " + aggressor.Profile.Nickname);
#endif

                var distance = Vector3.Distance(aggressor.Position, Position);
                mainPlayer.HandleTeammateKill(damageInfo, bodyPart, Side, role, ProfileId,
                    distance, Inventory.EquippedInSlotsTemplateIds, HealthController.BodyPartEffects, TriggerZones,
                    (FikaPlayer)aggressor);
            }
        }
    }

    public override void ExternalInteraction()
    {
        // Do nothing
    }

    public void CreateObservedCompass()
    {
        const string bundlePath = "assets/content/weapons/additional_hands/item_compass.bundle";
        if (!_compassLoaded)
        {
            var transform = Singleton<PoolManagerClass>.Instance.CreateFromPool<Transform>(new ResourceKey
            {
                path = bundlePath
            });
            transform.SetParent(PlayerBones.Ribcage.Original, false);
            transform.localRotation = Quaternion.identity;
            transform.localPosition = Vector3.zero;
            method_27(transform.gameObject);
            _compassLoaded = true;
        }
    }

    public void SetInventory(InventoryDescriptorClass inventoryDescriptor)
    {
        if (HandsController != null)
        {
            HandsController.FastForwardCurrentState();
        }

        var inventory = new EFTInventoryClass()
        {
            Equipment = inventoryDescriptor
        }.ToInventory();

        InventoryController.ReplaceInventory(inventory);
        if (CorpseSyncPacket.ItemSlot <= EquipmentSlot.Scabbard)
        {
            var heldItem = Equipment.GetSlot(CorpseSyncPacket.ItemSlot).ContainedItem;
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
        foreach (var equipmentSlot in PlayerBody.SlotNames)
        {
            var slot = Inventory.Equipment.GetSlot(equipmentSlot);
            ObservedSlotViewHandler handler = new(slot, this, equipmentSlot);
            _observedSlotViewHandlers.Add(handler);
        }

        if (PlayerBody.HaveHolster && PlayerBody.SlotViews.ContainsKey(EquipmentSlot.Holster))
        {
            var slot = Inventory.Equipment.GetSlot(EquipmentSlot.Holster);
            ObservedSlotViewHandler handler = new(slot, this, EquipmentSlot.Holster);
            _observedSlotViewHandlers.Add(handler);
        }

        if (HandsController != null && HandsController is ObservedFirearmController controller)
        {
            if (Inventory.Equipment.TryFindItem(controller.Weapon.Id, out var item))
            {
                if (item is not Weapon newWeapon)
                {
                    FikaGlobals.LogError("HandsController item was not Weapon");
                    return;
                }

                var newSlots = newWeapon.AllSlots;
                if (newSlots != null)
                {
                    Dictionary<string, GClass768.GClass769> currentViews = [];
                    foreach (var kvp in controller.CCV.ContainerBones)
                    {
                        if (kvp.Key is Slot slot && slot.ContainedItem != null)
                        {
                            if (currentViews.ContainsKey(slot.FullId))
                            {
                                FikaGlobals.LogError("CRITICAL ERROR DICTIONARY: " + slot.FullId);
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
                                var transform = TransformHelperClass.FindTransformRecursive(controller.CCV.GameObject.transform,
                                    slot.ID, true);
                                if (transform == null)
                                {
#if DEBUG
                                    FikaGlobals.LogWarning($"RefreshSlotViews::Transform was missing: {slot.ID}, this is harmless");
#endif
                                    continue;
                                }
                                controller.CCV.AddBone(slot, transform);
                                continue;
                            }
                            foreach (var kvp in currentViews)
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
        var playerTraverse = Traverse.Create(this);

        if (IsObservedAI)
        {
            BotStatePacket packet = new()
            {
                NetId = NetId,
                Type = BotStatePacket.EStateType.LoadBot
            };

            PacketSender.NetworkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered);

            var vaultingComponent = playerTraverse.Field<IVaultingComponent>("_vaultingComponent").Value;
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

            var vaultingComponent = playerTraverse.Field<IVaultingComponent>("_vaultingComponent").Value;
            if (vaultingComponent != null)
            {
                UpdateEvent -= vaultingComponent.DoVaultingTick;
            }
            playerTraverse.Field("_vaultingComponent").SetValue(null);
            playerTraverse.Field("_vaultingComponentDebug").SetValue(null);
            playerTraverse.Field("_vaultingParameters").SetValue(null);
            playerTraverse.Field("_vaultingGameplayRestrictions").SetValue(null);

            InitVaultingAudioControllers(_observedVaultingParameters);

            if (FikaPlugin.Instance.Settings.ShowNotifications.Value)
            {
                NotificationManagerClass.DisplayMessageNotification(string.Format(LocaleUtils.GROUP_MEMBER_SPAWNED.Localized(),
                    ColorizeText(EColor.GREEN, Profile.Info.MainProfileNickname)),
                EFT.Communications.ENotificationDurationType.Default, EFT.Communications.ENotificationIconType.Friend);
            }
        }
    }

    private IEnumerator CreateHealthBar()
    {
        var fikaGame = Singleton<IFikaGame>.Instance;
        if (fikaGame == null)
        {
            yield break;
        }

        while (fikaGame.GameController.GameInstance.Status != GameStatus.Started)
        {
            yield return null;
        }

        if (FikaPlugin.Instance.AllowNamePlates)
        {
            _healthBar = FikaHealthBar.Create(this);
        }

        while (Singleton<GameWorld>.Instance.MainPlayer == null)
        {
            yield return null;
        }
        Singleton<GameWorld>.Instance.MainPlayer.StatisticsManager.OnGroupMemberConnected(Inventory);
    }

    public override void LateUpdate()
    {
        DistanceDirty = true;
        OcclusionDirty = true;
        if (HealthController?.IsAlive != true)
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
            var instance = MonoBehaviourSingleton<BetterAudio>.Instance;
            var audioMixerGroup = Muffled ? instance.SimpleOccludedMixerGroup : instance.ObservedPlayerSpeechMixer;
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
        var compassInstantiated = Traverse.Create(this).Field<bool>("_compassInstantiated").Value;
        if (!compassInstantiated)
        {
            var transform = Singleton<PoolManagerClass>.Instance.CreateFromPool<Transform>(new ResourceKey
            {
                path = "assets/content/weapons/additional_hands/item_compass.bundle"
            });
            transform.SetParent(PlayerBones.Ribcage.Original, false);
            transform.localRotation = Quaternion.identity;
            transform.localPosition = Vector3.zero;
            method_27(transform.gameObject);
            Traverse.Create(this).Field("_compassInstantiated").SetValue(true);
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

    public override void SetControllerInsteadRemovedOne(Item removingItem, Callback callback)
    {
        callback.Succeed();
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

    public override void vmethod_0(WorldInteractiveObject interactiveObject, InteractionResult interactionResult, Action callback)
    {
        CurrentManagedState.StartDoorInteraction(interactiveObject, interactionResult, callback);
        UpdateInteractionCast();
    }

    public override void vmethod_1(WorldInteractiveObject door, InteractionResult interactionResult)
    {
        if (door != null)
        {
            CurrentManagedState.ExecuteDoorInteraction(door, interactionResult, null, this);
        }
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
        if (_followerCullingObject != null)
        {
            _followerCullingObject.enabled = false;
        }
        if (HandsController != null)
        {
            var handsController = HandsController;
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
        foreach (var slotViewHandler in _observedSlotViewHandlers)
        {
            slotViewHandler.Dispose();
        }
        _observedSlotViewHandlers.Clear();
        _observedCorpseCulling?.Dispose();
        if (HealthController.IsAlive)
        {
            if (!Singleton<IFikaNetworkManager>.Instance.ObservedPlayers.Remove(this) && !Profile.Nickname.StartsWith("headless_"))
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

    public void HandleProceedPacket(ProceedPacket packet)
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
        private readonly ObservedPlayer _observedPlayer;
        private readonly EquipmentSlot _slotType;

        public ObservedSlotViewHandler(Slot itemSlot, ObservedPlayer player, EquipmentSlot equipmentType)
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
            var slotBone = _observedPlayer.PlayerBody.GetSlotBone(_slotType);
            var alternativeHolsterBone = _observedPlayer.PlayerBody.GetAlternativeHolsterBone(_slotType);
            PlayerBody.EquipmentSlotClass newSlotView = new(_observedPlayer.PlayerBody, _slot, slotBone, _slotType,
                    _observedPlayer.Inventory.Equipment.GetSlot(EquipmentSlot.Backpack), alternativeHolsterBone, false);
            var oldSlotView = _observedPlayer.PlayerBody.SlotViews.AddOrReplace(_slotType, newSlotView);
            if (oldSlotView != null)
            {
                ClearSlotView(oldSlotView);
                oldSlotView.Dispose();
            }
            _observedPlayer.PlayerBody.ValidateHoodedDress(_slotType);
            GlobalEventHandlerClass.Instance.CreateCommonEvent<GClass3558>().Invoke(_observedPlayer.ProfileId);
            Dispose();
        }

        private void ClearSlotView(PlayerBody.EquipmentSlotClass oldSlotView)
        {
            for (var i = 0; i < oldSlotView.Renderers.Length; i++)
            {
                oldSlotView.Renderers[i].forceRenderingOff = false;
            }
            if (oldSlotView.Dresses != null)
            {
                var dresses = oldSlotView.Dresses;
                for (var j = 0; j < dresses.Length; j++)
                {
                    var bodyRenderer = dresses[j].GetBodyRenderer();
                    for (var k = 0; k < bodyRenderer.Renderers.Length; k++)
                    {
                        bodyRenderer.Renderers[k].forceRenderingOff = false;
                    }
                }
            }
        }
    }

    private void ObservedVisualPass(float deltaTime, int ikUpdateInterval)
    {
        if (CustomAnimationsAreProcessing || !_cullingHandler.IsVisible || !HealthController.IsAlive)
        {
            return;
        }

        _lastDistance = CameraClass.Instance.Distance(Transform.position);
        var isVisibleOrClose = IsVisible && _lastDistance <= EFTHardSettings.Instance.CULL_GROUNDER;

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
            const float num2 = 1f;
            var num4 = method_25(PlayerAnimator.LEFT_STANCE_CURVE);
            ProceduralWeaponAnimation.GetLeftStanceCurrentCurveValue(num4);
            _rightHand = 1f - (method_25(PlayerAnimator.RIGHT_HAND_WEIGHT) * num2);
            _leftHand = 1f - (method_25(PlayerAnimator.LEFT_HAND_WEIGHT) * num2);
            ThirdPersonWeaponRootAuthority = MovementContext.IsInMountedState ? 0f : (method_25(PlayerAnimator.WEAPON_ROOT_3RD) * num2);
            method_23(_lastDistance);
            if (_armsupdated)
            {
                var num5 = ThirdPersonWeaponRootAuthority;
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
            var num6 = method_25(PlayerAnimator.AIMING_LAYER_CURVE);
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
            var child = PlayerBones.Weapon_Root_Anim.GetChild(0);
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

    #region handControllers
    private void CreateHandsController(Func<AbstractHandsController> controllerFactory, Item item)
    {
        CreateHandsControllerHandler handler = new((item != null) ? method_137(item) : null);

        handler.SetInHandsOperation?.Confirm(true);

        if (HandsController != null)
        {
            var handsController = HandsController;
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
            if (_removeFromHandsCallback != null)
            {
                _removeFromHandsCallback.Invoke(SuccessfulResult.New);
                _removeFromHandsCallback = null;
            }
            HandsController = null;
        }

        base.SpawnController(controllerFactory(), handler.DisposeHandler);
        OnSetInHands(new(item, CommandStatus.Succeed, InventoryController));
        _shouldCullController = _handsController is EmptyHandsController || _handsController is KnifeController || _handsController is UsableItemController;
    }

    public void SpawnHandsController(EHandsControllerType controllerType, MongoID itemId, bool isStationary)
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
                FikaGlobals.LogWarning($"ObservedPlayer::SpawnHandsController: Unhandled ControllerType, was {controllerType}");
                break;
        }
    }

    private void CreateEmptyHandsController()
    {
        CreateHandsController(ReturnEmptyHandsController, null);
    }

    private AbstractHandsController ReturnEmptyHandsController()
    {
        return ObservedEmptyHandsController.Create(this);
    }

    private ObservedEmptyHandsController ProceedEmptyHandsController()
    {
        return ObservedEmptyHandsController.Create(this);
    }

    private void CreateFirearmController(MongoID itemId, bool isStationary = false, bool initial = false)
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
        var result = FindItemById(itemId, false, false);
        if (!result.Succeeded)
        {
            FikaGlobals.LogError(result.Error);
            return;
        }
        handler.item = result.Value;
        CreateHandsController(handler.ReturnController, handler.item);
    }

    private void CreateGrenadeController(MongoID itemId)
    {
        CreateGrenadeControllerHandler handler = new(this);

        var result = FindItemById(itemId, false, false);
        if (!result.Succeeded)
        {
            FikaGlobals.LogError(result.Error);
            return;
        }
        handler.Item = result.Value;
        if (handler.Item is ThrowWeapItemClass)
        {
            CreateHandsController(handler.ReturnController, handler.Item);
        }
        else
        {
            FikaGlobals.LogError($"CreateGrenadeController: Item was not of type GrenadeClass, was {handler.Item.GetType()}!");
        }
    }

    private void CreateMedsController(MongoID itemId, GStruct382<EBodyPart> bodyParts, float amount, int animationVariant)
    {
        var result = FindItemById(itemId, false, false);
        if (!result.Succeeded)
        {
            FikaGlobals.LogError(result.Error);
            return;
        }
        CreateMedsControllerHandler handler = new(this, result.Value, bodyParts, amount, animationVariant);
        CreateHandsController(handler.ReturnController, handler.Item);
    }

    private void CreateKnifeController(MongoID itemId)
    {
        CreateKnifeControllerHandler handler = new(this);
        var result = FindItemById(itemId, false, false);
        if (!result.Succeeded)
        {
            FikaGlobals.LogError(result.Error);
            return;
        }
        handler.Knife = result.Value.GetItemComponent<KnifeComponent>();
        if (handler.Knife != null)
        {
            CreateHandsController(handler.ReturnController, handler.Knife.Item);
        }
        else
        {
            FikaGlobals.LogError($"CreateKnifeController: Item did not contain a KnifeComponent, was of type {handler.Knife.GetType()}!");
        }
    }

    private void CreateQuickGrenadeController(MongoID itemId)
    {
        CreateQuickGrenadeControllerHandler handler = new(this);
        var result = FindItemById(itemId, false, false);
        if (!result.Succeeded)
        {
            FikaGlobals.LogError(result.Error);
            return;
        }
        handler.tem = result.Value;
        if (handler.tem is ThrowWeapItemClass)
        {
            CreateHandsController(handler.ReturnController, handler.tem);
        }
        else
        {
            FikaGlobals.LogError($"CreateQuickGrenadeController: Item was not of type GrenadeClass, was {handler.tem.GetType()}!");
        }
    }

    private void CreateQuickKnifeController(MongoID itemId)
    {
        CreateQuickKnifeControllerHandler handler = new(this);
        var result = FindItemById(itemId, false, false);
        if (!result.Succeeded)
        {
            FikaGlobals.LogError(result.Error);
            return;
        }
        handler.Knife = result.Value.GetItemComponent<KnifeComponent>();
        if (handler.Knife != null)
        {
            CreateHandsController(handler.ReturnController, handler.Knife.Item);
        }
        else
        {
            FikaGlobals.LogError($"CreateQuickKnifeController: Item did not contain a KnifeComponent, was of type {handler.Knife.GetType()}!");
        }
    }

    private void CreateUsableItemController(MongoID itemId)
    {
        var result = FindItemById(itemId, false, false);
        if (!result.Succeeded)
        {
            FikaGlobals.LogError(result.Error);
            return;
        }
        CreateUsableItemControllerHandler handler = new(this, result.Value);
        CreateHandsController(handler.ReturnController, handler.Item);
    }

    private void CreateQuickUseItemController(MongoID itemId)
    {
        var result = FindItemById(itemId, false, false);
        if (!result.Succeeded)
        {
            FikaGlobals.LogError(result.Error);
            return;
        }
        CreateQuickUseItemControllerHandler handler = new(this, result.Value);
        CreateHandsController(handler.ReturnController, handler.Item);
    }

    public void SetAggressorData(MongoID? killerId, EBodyPart bodyPart, MongoID? weaponId)
    {
        var killer = Singleton<GameWorld>.Instance.GetEverExistedPlayerByID(killerId);
        if (killer != null)
        {
            LastAggressor = killer;
        }
        LastBodyPart = bodyPart;
        _lastWeaponId = weaponId;

        if (LastDamageInfo.Weapon == null && _lastWeaponId != null)
        {
            FindKillerWeapon();
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

    private class RemoveHandsControllerHandler(ObservedPlayer fikaPlayer, Callback callback)
    {
        private readonly ObservedPlayer _fikaPlayer = fikaPlayer;
        private readonly Callback _callback = callback;

        public void Handle(Result<GInterface198> result)
        {
            if (_fikaPlayer._removeFromHandsCallback == _callback)
            {
                _fikaPlayer._removeFromHandsCallback = null;
            }
            _callback.Invoke(result);
        }
    }

    private class CreateHandsControllerHandler(Class1310 setInHandsOperation)
    {
        public readonly Class1310 SetInHandsOperation = setInHandsOperation;

        internal void DisposeHandler()
        {
            var handler = SetInHandsOperation;
            if (handler == null)
            {
                return;
            }
            handler.Dispose();
        }
    }

    private class CreateFirearmControllerHandler(ObservedPlayer fikaPlayer)
    {
        private readonly ObservedPlayer _fikaPlayer = fikaPlayer;
        public Item item;

        internal AbstractHandsController ReturnController()
        {
            return ObservedFirearmController.Create(_fikaPlayer, (Weapon)item);
        }
    }

    private class CreateGrenadeControllerHandler(ObservedPlayer fikaPlayer)
    {
        private readonly ObservedPlayer _fikaPlayer = fikaPlayer;
        public Item Item;

        internal AbstractHandsController ReturnController()
        {
            return ObservedGrenadeController.Create(_fikaPlayer, (ThrowWeapItemClass)Item);
        }
    }

    private class CreateMedsControllerHandler(ObservedPlayer fikaPlayer, Item item, GStruct382<EBodyPart> bodyParts, float amount, int animationVariant)
    {
        private readonly ObservedPlayer _fikaPlayer = fikaPlayer;
        public readonly Item Item = item;
        private readonly GStruct382<EBodyPart> _bodyParts = bodyParts;
        private readonly float _amount = amount;
        private readonly int _animationVariant = animationVariant;

        internal AbstractHandsController ReturnController()
        {
            return ObservedMedsController.Create(_fikaPlayer, Item, _bodyParts, _amount, _animationVariant);
        }
    }

    private class CreateKnifeControllerHandler(ObservedPlayer fikaPlayer)
    {
        private readonly ObservedPlayer _fikaPlayer = fikaPlayer;
        public KnifeComponent Knife;

        internal AbstractHandsController ReturnController()
        {
            return ObservedKnifeController.Create(_fikaPlayer, Knife);
        }
    }

    private class CreateQuickGrenadeControllerHandler(ObservedPlayer fikaPlayer)
    {
        private readonly ObservedPlayer _fikaPlayer = fikaPlayer;
        public Item tem;

        internal AbstractHandsController ReturnController()
        {
            return ObservedQuickGrenadeController.Create(_fikaPlayer, (ThrowWeapItemClass)tem);
        }
    }

    private class CreateQuickKnifeControllerHandler(ObservedPlayer fikaPlayer)
    {
        private readonly ObservedPlayer _fikaPlayer = fikaPlayer;
        public KnifeComponent Knife;

        internal AbstractHandsController ReturnController()
        {
            return QuickKnifeKickController.smethod_9<QuickKnifeKickController>(_fikaPlayer, Knife);
        }
    }

    private class CreateUsableItemControllerHandler(ObservedPlayer fikaPlayer, Item item)
    {
        private readonly ObservedPlayer _fikaPlayer = fikaPlayer;
        public readonly Item Item = item;

        internal AbstractHandsController ReturnController()
        {
            return UsableItemController.smethod_6<UsableItemController>(_fikaPlayer, Item);
        }
    }

    private class CreateQuickUseItemControllerHandler(ObservedPlayer fikaPlayer, Item item)
    {
        private readonly ObservedPlayer _fikaPlayer = fikaPlayer;
        public readonly Item Item = item;

        internal AbstractHandsController ReturnController()
        {
            return QuickUseItemController.smethod_6<QuickUseItemController>(_fikaPlayer, Item);
        }
    }
}

#endregion