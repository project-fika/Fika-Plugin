// © 2026 Lacyway All Rights Reserved

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Audio.SpatialSystem;
using Comfort.Common;
using Dissonance;
using EFT;
using EFT.Ballistics;
using EFT.CameraControl;
using EFT.Communications;
using EFT.Dialogs;
using EFT.GlobalEvents;
using EFT.HealthSystem;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.NetworkPackets;
using EFT.NextObservedPlayer;
using EFT.Settings;
using EFT.Settings.Sound;
using EFT.Vaulting;
using Fika.Core.Main.Components;
using Fika.Core.Main.Factories;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.ObservedClasses;
using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.PacketHandlers;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.Communication;
using Fika.Core.Networking.Packets.Player;
using Fika.Core.Networking.Packets.Player.Common;
using Fika.Core.Networking.Packets.Player.Common.SubPackets;
using Fika.Core.Networking.Snapshotting;
using JsonType;
using static Fika.Core.UI.FikaUIGlobals;

namespace Fika.Core.Main.Players;

/// <summary>
/// Observed players are any other players in the world for a client, including bots. <br/>
/// Bots are handled by the server, and other clients send their own data which the server replicates to other clients. <br/>
/// As a host all <see cref="ObservedPlayer"/>s are only other clients.
/// </summary>
public sealed class ObservedPlayer : FikaPlayer
{
    #region Fields and Properties
    /// <summary>
    /// Gets the health bar UI component attached to this observed player.
    /// </summary>
    public FikaHealthBar HealthBar
    {
        get
        {
            return _healthBar;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether weapon and obstacle overlap should be recalculated.
    /// </summary>
    public bool ShouldOverlap { get; internal set; }

    /// <summary>
    /// Gets or sets a value indicating whether left shoulder stance is disabled on this player.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the spatial audio source used for VOIP playback from this player.
    /// </summary>
    public BetterSource VoipEftSource { get; set; }

    /// <summary>
    /// Gets the health controller cast to <see cref="ObservedHealthController"/> for network health synchronization.
    /// </summary>
    public ObservedHealthController NetworkHealthController
    {
        get
        {
            return HealthController as ObservedHealthController;
        }
    }

    /// <summary>
    /// Gets a value indicating whether this player can be snapped to geometry. Always returns <see langword="false"/> for observed players.
    /// </summary>
    public override bool CanBeSnapped
    {
        get
        {
            return false;
        }
    }

    /// <summary>
    /// Gets or sets the active point of view, updating player rendering, bones scaling, and animations.
    /// </summary>
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
            CalculateScaleValueByFov((float)Singleton<SettingsManager>.Instance.Game.Settings.FieldOfView);
            SetCompensationScale(false);
            PlayerBones.Ribcage.Original.localScale = new Vector3(1f, 1f, 1f);
            MovementContext.PlayerAnimatorPointOfView(value);
            var pointOfViewChanged = PointOfViewChanged;
            pointOfViewChanged?.Invoke();
            _playerBody.UpdatePlayerRenders(_playerBody.PointOfView.Value, Side);
            ProceduralWeaponAnimation.PointOfView = value;
        }
    }

    /// <summary>
    /// Gets or sets the hands controller and notifies the movement context animator of weapon changes.
    /// </summary>
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

    /// <summary>
    /// Gets the ray used for interaction checks cast from the player's view direction.
    /// </summary>
    public override Ray InteractionRay
    {
        get
        {
            var vector = HandsRotation * Vector3.forward;
            return new(_playerLookRaycastTransform.position, vector);
        }
    }

    /// <summary>
    /// Gets the hearing range multiplier for audio effects relative to the local protagonist hearing setting.
    /// </summary>
    public override float ProtagonistHearing
    {
        get
        {
            return Mathf.Max(1f, Singleton<BetterAudio>.Instance.ProtagonistHearing + 1f);
        }
    }

    /// <summary>
    /// Gets or sets whether this observed player is currently visible according to follower culling (always <see langword="true"/> in headless mode).
    /// </summary>
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

    /// <summary>
    /// Gets the character controller attached to the movement context.
    /// </summary>
    public ImpostorCharacterController ObservedCharacterController
    {
        get
        {
            return MovementContext.ImpostorCC;
        }
    }

    /// <summary>
    /// Timestamp (in seconds) after which Full Body Biped IK (FBBIK) quick mode is reactivated following hit reactions.
    /// </summary>
    public float TurnOffFbbikAt;

    /// <summary>
    /// Current interpolated/extrapolated replicated player state snapshot.
    /// </summary>
    internal ObservedState CurrentPlayerState;
    /// <summary>
    /// Cached distance in meters from the camera to this observed player on the previous frame.
    /// </summary>
    private float _lastDistance;
    /// <summary>
    /// Weight applied to the right hand IK solver.
    /// </summary>
    private float _rightHand;
    /// <summary>
    /// Weight applied to the left hand IK solver.
    /// </summary>
    private float _leftHand;
    /// <summary>
    /// Indicates whether the active hands controller model and animator should be culled at a distance.
    /// </summary>
    private bool _shouldCullController;
    /// <summary>
    /// Handlers monitoring equipment slot changes to update third-person visual meshes.
    /// </summary>
    private readonly List<ObservedSlotViewHandler> _observedSlotViewHandlers = [];
    /// <summary>
    /// Manages distance and frustum culling for this player's corpse after death.
    /// </summary>
    private ObservedCorpseCulling _observedCorpseCulling;
    /// <summary>
    /// Follower culling component used to determine player visibility.
    /// </summary>
    private FollowerCullingObject _followerCullingObject;
    /// <summary>
    /// Replicated vaulting parameters specifying obstacle heights, speed, and weights.
    /// </summary>
    private readonly ObservedVaultingParameters _observedVaultingParameters = new();
    /// <summary>
    /// Backing field for <see cref="LeftStanceDisabled"/>.
    /// </summary>
    private bool _leftStancedDisabled;
    /// <summary>
    /// Backing field for <see cref="HealthBar"/>.
    /// </summary>
    private FikaHealthBar _healthBar;
    /// <summary>
    /// Cached flag indicating if this instance is running as the server/host.
    /// </summary>
    private bool _isServer;
    /// <summary>
    /// Trigger component used for spatial VOIP broadcast via Dissonance.
    /// </summary>
    private VoiceBroadcastTrigger _voiceBroadcastTrigger;
    /// <summary>
    /// Cached sound settings group used for voice volume bindings.
    /// </summary>
    private SoundSettingsGroup _soundSettings;
    /// <summary>
    /// Interactable component added to this player when downed to allow teammates to revive them.
    /// </summary>
    private ReviveInteractable _reviveInteractable;
    /// <summary>
    /// Indicates whether the VOIP audio source binding and occlusion processing have completed.
    /// </summary>
    private bool _voipAssigned;
    /// <summary>
    /// Frame skip offset modulo used to stagger tick operations across observed players.
    /// </summary>
    private int _frameSkip;
    /// <summary>
    /// Indicates whether this player uses the simplified zombie animation rig and skeleton.
    /// </summary>
    private bool _isZombie;

    /// <summary>
    /// Squared threshold below which movement direction is treated as stationary.
    /// </summary>
    private const float _movementDeadZoneSqr = 0.05f * 0.05f;
    /// <summary>
    /// Squared threshold below which interpolated velocity is clamped to zero.
    /// </summary>
    private const float _velocityDeadZoneSqr = 0.20f * 0.20f;
    #endregion

    /// <summary>
    /// Asynchronously creates, configures, and spawns a new <see cref="ObservedPlayer"/> instance.
    /// </summary>
    /// <param name="gameWorld">The active EFT game world instance.</param>
    /// <param name="playerId">The unique player network ID.</param>
    /// <param name="position">Initial world spawn position.</param>
    /// <param name="rotation">Initial rotation quaternion.</param>
    /// <param name="layerName">Layer name to assign to the player object.</param>
    /// <param name="prefix">Prefab name prefix.</param>
    /// <param name="pointOfView">Initial point of view (typically third person).</param>
    /// <param name="profile">Player profile containing skills, stats, and inventory data.</param>
    /// <param name="healthBytes">Serialized health controller snapshot data.</param>
    /// <param name="aiControl"><see langword="true"/> if this observed player is controlled by AI.</param>
    /// <param name="updateQueue">Update queue channel.</param>
    /// <param name="armsUpdateMode">Animator update mode for arms.</param>
    /// <param name="bodyUpdateMode">Animator update mode for body.</param>
    /// <param name="characterControllerMode">Character controller spawning mode.</param>
    /// <param name="getSensitivity">Delegate retrieving mouse sensitivity.</param>
    /// <param name="getAimingSensitivity">Delegate retrieving aiming mouse sensitivity.</param>
    /// <param name="filter">Customization filter for appearance.</param>
    /// <param name="firstId">Starting MongoID for inventory operations.</param>
    /// <param name="firstOperationId">Starting network operation sequence ID.</param>
    /// <param name="isZombie"><see langword="true"/> if the player is an infected/zombie entity using simple animators.</param>
    /// <returns>A task representing the asynchronous creation operation, resolving to the initialized <see cref="ObservedPlayer"/>.</returns>
    public static async Task<ObservedPlayer> CreateObservedPlayer(GameWorld gameWorld, int playerId, Vector3 position, Quaternion rotation, string layerName,
        string prefix, EPointOfView pointOfView, Profile profile, byte[] healthBytes, bool aiControl,
        EUpdateQueue updateQueue, EUpdateMode armsUpdateMode, EUpdateMode bodyUpdateMode,
        CharacterControllerSpawner.Mode characterControllerMode, Func<float> getSensitivity, Func<float> getAimingSensitivity,
        ICustomizationFilter filter, MongoID firstId, ushort firstOperationId, bool isZombie)
    {
        var useSimpleAnimator = isZombie;
#if DEBUG
        if (useSimpleAnimator)
        {
            FikaGlobals.LogWarning("Using SimpleAnimator!");
        }
#endif
        var resourceKey = useSimpleAnimator ? InGameBundles.ZOMBIE_BUNDLE_NAME : InGameBundles.PLAYER_BUNDLE_NAME;
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
        NetworkDialogueController dialogueController = null;
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
        player._handsController = EmptyHandsController.CreateController<EmptyHandsController>(player);
        player._handsController.Spawn(1f, FikaGlobals.EmptyAction);

        player.AIData = new AIData(null, player);

        player.botPlayerCulling = new();
        player.botPlayerCulling.Initialize(player, player.PlayerBones);

        if (FikaBackendUtils.IsHeadless || profile.IsPlayerProfile())
        {
            player.botPlayerCulling.Disable();
        }

        if (FikaBackendUtils.IsHeadless)
        {
            player.EnabledAnimators = EAnimatorMask.Thirdperson | EAnimatorMask.Arms;
        }

        if (!aiControl)
        {
            var services = player._notYetPurchasedTraderServiceTypes;
            foreach (var etraderServiceType in Singleton<GlobalConfiguration>.Instance.ServicesData.Keys)
            {
                services.Add(etraderServiceType);
            }
        }

        player.AggressorFound = false;
        player._animators[0].enabled = true;
        player._isServer = FikaBackendUtils.IsServer;
        player.Snapshotter = new PlayerSnapshotter<PlayerStateSnapshot>();
        player.CurrentPlayerState = new ObservedState(position, player.Rotation);
        player._isZombie = player.UsedSimplifiedSkeleton;

        player._frameSkip = ObservedPlayerController._evenOrNotEvenUpdateLastValue;
        ObservedPlayerController._evenOrNotEvenUpdateLastValue = (ObservedPlayerController._evenOrNotEvenUpdateLastValue + 1) % 3;

        CameraManager.Instance.FoVUpdateAction -= player.OnFovUpdatedEvent;

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
        player.NetId = playerId;

        OnPlayerSpawned?.Invoke(player);

        return player;
    }

    /// <summary>
    /// Gets the body transform used as the tracking origin for follower culling.
    /// </summary>
    /// <returns>The original body transform from <see cref="PlayerBones"/>.</returns>
    private Transform GetCullingTransform()
    {
        return PlayerBones.BodyTransform.Original;
    }

    /// <summary>
    /// Disposes vision and face cover observers that are redundant on remote observed players.
    /// </summary>
    private void DisposeObservers()
    {
        NightVisionObserver.Dispose();
        ThermalVisionObserver.Dispose();
        FaceCoverObserver.Dispose();
        FaceCoverObserver.Dispose();
    }

    /// <summary>
    /// Initializes positional VOIP communications and audio sources for this player.
    /// </summary>
    /// <param name="voipState">The VOIP state indicating availability.</param>
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

    /// <summary>
    /// Coroutine that waits for the Dissonance audio source to be assigned and configures spatial audio effects.
    /// </summary>
    /// <returns>An <see cref="IEnumerator"/> for coroutine execution.</returns>
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

    /// <summary>
    /// Attaches and configures the <see cref="VoiceBroadcastTrigger"/> component for VOIP playback.
    /// </summary>
    private void SetupVoiceBroadcastTrigger()
    {
        _voiceBroadcastTrigger = gameObject.AddComponent<VoiceBroadcastTrigger>();
        _voiceBroadcastTrigger.ChannelType = CommTriggerTarget.Self;
        _soundSettings = Singleton<SettingsManager>.Instance.Sound.Settings;
        CompositeDisposable.BindState(_soundSettings.VoiceChatVolume, ChangeVoipDeviceSensitivity);
    }

    /// <summary>
    /// Updates the voice broadcast trigger fader volume when the user's voice volume setting changes.
    /// </summary>
    /// <param name="value">The sound volume level from 0 to 100.</param>
    private void ChangeVoipDeviceSensitivity(int value)
    {
        _voiceBroadcastTrigger.ActivationFader.Volume = (float)value / 100f;
    }

    /// <summary>
    /// Creates a lightweight physical parameter instance without stamina drain simulation.
    /// </summary>
    /// <returns>A new <see cref="PhysicalBase"/> instance.</returns>
    public override PhysicalBase CreatePhysical()
    {
        return new PhysicalBase();
    }

    /// <summary>
    /// Triggers a spoken voice phrase if the player object is active in the hierarchy.
    /// </summary>
    /// <param name="phrase">Phrase trigger type to speak.</param>
    /// <param name="demand">Whether the voice line is demanded immediately.</param>
    /// <param name="delay">Playback delay in seconds.</param>
    /// <param name="mask">Tag mask filters.</param>
    /// <param name="probability">Probability percentage of speaking (0-100).</param>
    /// <param name="aggressive">Whether to play aggressive vocalization variant.</param>
    public override void Say(EPhraseTrigger phrase, bool demand = false, float delay = 0, ETagStatus mask = 0, int probability = 100, bool aggressive = false)
    {
        if (gameObject.activeSelf)
        {
            base.Say(phrase, demand, delay, mask, probability, aggressive);
        }
    }

    /// <summary>
    /// Plays landing and grounded impact sounds based on the calculated surface type beneath the player.
    /// </summary>
    /// <param name="fallHeight">Height fallen before landing.</param>
    /// <param name="jumpHeight">Jump height reached.</param>
    public override void PlayGroundedSound(float fallHeight, float jumpHeight)
    {
        (var hit, var surfaceSound) = CalculateMovementSurface();
        UpdateSurfaceData(hit, surfaceSound);
        base.PlayGroundedSound(fallHeight, jumpHeight);
    }

    /// <summary>
    /// Overridden to suppress skill level change notifications on observed players.
    /// </summary>
    /// <param name="skill">The skill whose level changed.</param>
    public override void OnSkillLevelChanged(BaseSkill skill)
    {
        // Do nothing
    }

    /// <summary>
    /// Overridden to suppress weapon mastery notifications on observed players.
    /// </summary>
    /// <param name="masterSkill">The weapon mastering skill updated.</param>
    public override void OnWeaponMastered(Mastering masterSkill)
    {
        // Do nothing
    }

    /// <summary>
    /// Overridden to prevent self-damage coroutines from running on observed players.
    /// </summary>
    public override void StartInflictSelfDamageCoroutine()
    {
        // Do nothing
    }

    /// <summary>
    /// Overridden to suppress state speed limit additions on observed players.
    /// </summary>
    /// <param name="speedDelta">Speed difference delta.</param>
    /// <param name="cause">The cause of the speed limit.</param>
    public override void AddStateSpeedLimit(float speedDelta, ESpeedLimit cause)
    {
        // Do nothing
    }

    /// <summary>
    /// Overridden to suppress speed limit updates on observed players.
    /// </summary>
    /// <param name="speedDelta">Speed difference delta.</param>
    /// <param name="cause">The cause of the speed limit.</param>
    public override void UpdateSpeedLimit(float speedDelta, ESpeedLimit cause)
    {
        // Do nothing
    }

    /// <summary>
    /// Overridden to suppress health-dependent speed limit updates on observed players.
    /// </summary>
    public override void UpdateSpeedLimitByHealth()
    {
        // Do nothing
    }

    /// <summary>
    /// Overridden to suppress timed speed limit updates on observed players.
    /// </summary>
    /// <param name="speedDelta">Speed difference delta.</param>
    /// <param name="cause">The cause of the speed limit.</param>
    /// <param name="duration">Duration of the limit in seconds.</param>
    public override void UpdateSpeedLimit(float speedDelta, ESpeedLimit cause, float duration)
    {
        // Do nothing
    }

    /// <summary>
    /// Overridden to suppress tactical headset audio filter updates on observed players.
    /// </summary>
    public override void UpdatePhones()
    {
        // Do nothing
    }

    /// <summary>
    /// Overridden to suppress face shield mark operations on observed players.
    /// </summary>
    /// <param name="armor">The face shield component.</param>
    /// <param name="hasServerOrigin">Whether the operation originated from the server.</param>
    public override void FaceshieldMarkOperation(FaceShieldComponent armor, bool hasServerOrigin)
    {
        // Do nothing
    }

    /*public override void ShotReactions(DamageInfo shot, EBodyPart bodyPart)
    {
        TurnOffFbbikAt = Time.time + 0.6f;
        base.ShotReactions(shot, bodyPart);
    }*/

    /// <summary>
    /// Updates aggressor statistics, triggers weapon skill progression on the damaging player, and checks death status.
    /// </summary>
    /// <param name="DamageInfo">Information detailing the inflicted damage.</param>
    /// <param name="bodyPart">The affected body part.</param>
    /// <param name="colliderType">The collider type that was hit.</param>
    public override void ManageAggressor(DamageInfo DamageInfo, EBodyPart bodyPart, EBodyPartColliderType colliderType)
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
            var flag = DamageInfo.DidBodyDamage / HealthController.GetBodyPartHealth(bodyPart, false).Maximum >= 0.6f && HealthController.FindExistingEffect<IBleeding>(bodyPart) != null;
            player.StatisticsManager.OnEnemyDamage(DamageInfo, bodyPart, ProfileId, Side, Profile.Info.Settings.Role,
                GroupId, HealthController.GetBodyPartHealth(EBodyPart.Common, false).Maximum, flag,
                Vector3.Distance(player.Transform.position, Transform.position), CurrentHour,
                Inventory.EquippedInSlotsTemplateIds, HealthController.BodyPartEffects, TriggerZones);
        }
    }

    /// <summary>
    /// Overridden to suppress arm stamina and condition updates on observed players.
    /// </summary>
    public override void UpdateArmsCondition()
    {
        // Do nothing
    }

    /// <summary>
    /// Determines whether the player should play death vocalization screams depending on the fatal hit location.
    /// </summary>
    /// <param name="bodyPart">The fatal body part hit.</param>
    /// <returns><see langword="true"/> if the hit was not to the head; otherwise, <see langword="false"/>.</returns>
    public override bool ShouldVocalizeDeath(EBodyPart bodyPart)
    {
        return bodyPart > EBodyPart.Head;
    }

    /// <summary>
    /// Overridden to suppress headlight toggle network packets from observed players.
    /// </summary>
    /// <param name="isSilent">Whether the toggle was silent.</param>
    public override void SendHeadlightsPacket(bool isSilent)
    {
        // Do nothing
    }

    /// <summary>
    /// Triggers pain vocalizations, increases awareness for AI, and updates hit reaction state.
    /// </summary>
    /// <param name="damage">Amount of damage taken.</param>
    /// <param name="staminaBurnRate">Rate at which stamina was depleted.</param>
    /// <param name="bodyPartType">The body part that received damage.</param>
    /// <param name="damageType">The category of damage inflicted.</param>
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

    /// <summary>
    /// Handles explosive damage by updating damage history and transmitting a damage packet to the network.
    /// </summary>
    /// <param name="DamageInfo">Information detailing the explosion damage.</param>
    /// <param name="bodyPartType">Target body part hit by the explosion.</param>
    /// <param name="colliderType">Collider type hit.</param>
    public void HandleExplosive(DamageInfo DamageInfo, EBodyPart bodyPartType, EBodyPartColliderType colliderType)
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
        CommonPacket.SubPacket.Damage = new DamagePacket(NetId, DamageInfo, bodyPartType, colliderType);
        Singleton<IFikaNetworkManager>.Instance.SendData(ref CommonPacket, DeliveryMethod.ReliableOrdered, true);
    }

    /// <summary>
    /// Records damage parameters and last aggressor references on this observed player.
    /// </summary>
    /// <param name="DamageInfo">Information detailing the damage.</param>
    /// <param name="bodyPartType">The damaged body part.</param>
    /// <param name="colliderType">The collider type that was struck.</param>
    /// <param name="absorbed">Amount of damage absorbed by armor.</param>
    public override void ApplyDamageInfo(DamageInfo DamageInfo, EBodyPart bodyPartType, EBodyPartColliderType colliderType, float absorbed)
    {
        LastAggressor = DamageInfo.Player.iPlayer;
        LastDamagedBodyPart = bodyPartType;
        LastBodyPart = bodyPartType;
        LastDamageInfo = DamageInfo;
        LastDamageType = DamageInfo.DamageType;
    }

    /// <summary>
    /// Handles out-of-bounds sniper shot damage, applies debuffs, and broadcasts a network damage packet.
    /// </summary>
    /// <param name="DamageInfo">Damage information from the sniper shot.</param>
    /// <param name="bodyPartType">Target body part hit.</param>
    /// <param name="colliderType">Collider type struck.</param>
    /// <param name="armorPlateCollider">Armor plate collider hit, if applicable.</param>
    /// <param name="shotId">Unique shot identifier.</param>
    /// <returns>A <see cref="PlayerHitInfo"/> describing the impact, or <see langword="null"/> if damage coefficient is 0.</returns>
    public PlayerHitInfo HandleSniperShot(DamageInfo DamageInfo, EBodyPart bodyPartType, EBodyPartColliderType colliderType, EArmorPlateCollider armorPlateCollider, ShotId shotId)
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
        CommonPacket.SubPacket.Damage = new DamagePacket(NetId, DamageInfo, bodyPartType, colliderType, armorPlateCollider);
        Singleton<IFikaNetworkManager>.Instance.SendData(ref CommonPacket, DeliveryMethod.ReliableOrdered, true);

        return new()
        {
            PoV = EPointOfView.ThirdPerson,
            Penetrated = DamageInfo.Penetrated,
            Material = MaterialType.Body
        };
    }

    /// <summary>
    /// Processes incoming ballistic bullet damage, applies armor penetration and degradation, updates health, and syncs over network.
    /// </summary>
    /// <param name="damageInfo">Damage information describing the ballistic hit.</param>
    /// <param name="bodyPartType">Target body part hit.</param>
    /// <param name="colliderType">Collider type struck.</param>
    /// <param name="armorPlateCollider">Specific armor plate collider hit.</param>
    /// <param name="shotId">Unique shot identifier.</param>
    /// <returns>A <see cref="PlayerHitInfo"/> summarizing the hit result, or <see langword="null"/> if already dead.</returns>
    public override PlayerHitInfo ApplyShot(DamageInfo damageInfo, EBodyPart bodyPartType, EBodyPartColliderType colliderType, EArmorPlateCollider armorPlateCollider, ShotId shotId)
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
        PlayerHitInfo hitInfo = new()
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
        CommonPacket.SubPacket.Damage = new DamagePacket(NetId, damageInfo, bodyPartType, colliderType, armorPlateCollider, absorbed: num);
        Singleton<IFikaNetworkManager>.Instance.SendData(ref CommonPacket, DeliveryMethod.ReliableOrdered, true);

        // Run this to get weapon skill
        ManageAggressor(damageInfo, bodyPartType, colliderType);

        return hitInfo;
    }

    /// <summary>
    /// Overridden to suppress local inventory add/remove events on observed players.
    /// </summary>
    /// <param name="item">The item added or removed.</param>
    /// <param name="location">Item inventory location.</param>
    /// <param name="added"><see langword="true"/> if added; <see langword="false"/> if removed.</param>
    public override void OnItemAddedOrRemoved(Item item, ItemAddress location, bool added)
    {
        // Do nothing
    }

    /// <summary>
    /// Applies explosion durability damage to equipped armor components on the server.
    /// </summary>
    /// <param name="armorDamage">Dictionary mapping explosion hits to durability damage amounts.</param>
    /// <param name="DamageInfo">Explosion damage information.</param>
    public override void ApplyExplosionDamageToArmor(Dictionary<ExplosionDamageInfo, float> armorDamage, DamageInfo DamageInfo)
    {
        if (!_isServer)
        {
            return;
        }

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
                OnArmorDamaged(num, armorComponent);
                OnArmorPointsChanged(armorComponent);
            }
        }
    }

    /// <summary>
    /// Processes client-side simulated ballistic damage, updating aggressor records, armor durability, and network synchronization.
    /// </summary>
    /// <param name="damageInfo">Damage info received from client calculation.</param>
    /// <param name="bodyPartType">Target body part struck.</param>
    /// <param name="colliderType">Collider struck.</param>
    /// <param name="armorPlateCollider">Armor plate collider hit.</param>
    /// <param name="shotId">Unique shot identifier.</param>
    /// <returns>A <see cref="PlayerHitInfo"/> summarizing the hit result, or <see langword="null"/> if dead.</returns>
    public PlayerHitInfo ApplyClientShot(DamageInfo damageInfo, EBodyPart bodyPartType, EBodyPartColliderType colliderType, EArmorPlateCollider armorPlateCollider, ShotId shotId)
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
        PlayerHitInfo hitInfo = new()
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
        CommonPacket.SubPacket.Damage = new DamagePacket(NetId, damageInfo, bodyPartType, colliderType, armorPlateCollider, absorbed: num);
        Singleton<IFikaNetworkManager>.Instance.SendData(ref CommonPacket, DeliveryMethod.ReliableOrdered, true);

        // Run this to get weapon skill
        ManageAggressor(damageInfo, bodyPartType, colliderType);

        return hitInfo;
    }

    /// <summary>
    /// Overridden to suppress weapon mounting commands on observed players.
    /// </summary>
    /// <param name="command">The mounting command.</param>
    public override void OnMounting(EFT.MountingPacket.EMountingCommand command)
    {
        // Do nothing
    }

    /// <summary>
    /// Applies physics impulse to this player's corpse ragdoll according to synchronized death packet data.
    /// </summary>
    public override void ApplyCorpseImpulse()
    {
        if (botPlayerCulling.IsVisible || _isServer)
        {
            if (CorpseSyncPacket.BodyPartColliderType != EBodyPartColliderType.None
                    && PlayerBones.BodyPartCollidersDictionary.TryGetValue(CorpseSyncPacket.BodyPartColliderType, out var bodyPartCollider))
            {
                Corpse.Ragdoll.ApplyImpulse(bodyPartCollider.Collider, CorpseSyncPacket.Direction, CorpseSyncPacket.Point, CorpseSyncPacket.Force);
            }
        }
    }

    /// <summary>
    /// Creates the <see cref="ObservedMovementContext"/> used to animate and position this remote observed player.
    /// </summary>
    public override void CreateMovementContext()
    {
        MovementContext = ObservedMovementContext.Create(this, GetBodyAnimatorCommon,
            GetCharacterControllerCommon, EFTHardSettings.Instance.MOVEMENT_MASK);
    }

    /// <summary>
    /// Plays environmental sound effects (such as bone fractures) when a health effect is added.
    /// </summary>
    /// <param name="effect">The health effect added.</param>
    public override void OnHealthEffectAdded(IHealthEffect effect)
    {
        if (effect is IFracture fracture && !fracture.WasPaused && FractureSound != null && Singleton<BetterAudio>.Instantiated)
        {
            Singleton<BetterAudio>.Instance.PlayAtPoint(Position, FractureSound, CameraManager.Instance.Distance(Position),
                BetterAudio.AudioSourceGroupType.Impacts, 15, 0.7f, EOcclusionTest.Fast, null, false);
        }
    }

    /// <summary>
    /// Overridden to suppress health effect removal handling on observed players.
    /// </summary>
    /// <param name="effect">The health effect removed.</param>
    public override void OnHealthEffectRemoved(IHealthEffect effect)
    {
        // Do nothing
    }

    /// <summary>
    /// Overridden to prevent skill manager event connections on observed players.
    /// </summary>
    public override void ConnectSkillManager()
    {
        // Do nothing
    }

    #region proceed
    /// <summary>
    /// Transitions hands to an empty hands controller.
    /// </summary>
    /// <param name="withNetwork">Whether to synchronize over network.</param>
    /// <param name="callback">Callback invoked upon completion.</param>
    /// <param name="scheduled">Whether the transition is scheduled.</param>
    public override void Proceed(bool withNetwork, Callback<IEmptyHandsController> callback, bool scheduled = true)
    {
        Func<EmptyHandsController> func = new(ProceedEmptyHandsController);
        new Process<EmptyHandsController, IEmptyHandsController>(this, func, null, false)
            .Proceed(null, callback, scheduled);
    }

    /// <summary>
    /// Transitions hands to a knife melee controller.
    /// </summary>
    /// <param name="knife">The knife component to equip.</param>
    /// <param name="callback">Callback invoked upon completion.</param>
    /// <param name="scheduled">Whether the transition is scheduled.</param>
    public override void Proceed(KnifeComponent knife, Callback<IKnifeController> callback, bool scheduled = true)
    {
        HandsControllerFactory factory = new(this, knifeComponent: knife);
        Func<KnifeController> func = new(factory.CreateObservedKnifeController);
        new Process<KnifeController, IKnifeController>(this, func, factory.KnifeComponent.Item)
            .Proceed(null, callback, scheduled);
    }

    /// <summary>
    /// Transitions hands to a throwable grenade controller.
    /// </summary>
    /// <param name="throwWeap">The throwable grenade item to equip.</param>
    /// <param name="callback">Callback invoked upon completion.</param>
    /// <param name="scheduled">Whether the transition is scheduled.</param>
    public override void Proceed(ThrowWeap throwWeap, Callback<IGrenadeController> callback, bool scheduled = true)
    {
        HandsControllerFactory factory = new(this, throwWeap);
        Func<GrenadeHandsController> func = new(factory.CreateObservedGrenadeController);
        new Process<GrenadeHandsController, IGrenadeController>(this, func, throwWeap, false)
            .Proceed(null, callback, scheduled);
    }

    /// <summary>
    /// Transitions hands to a quick grenade throw controller.
    /// </summary>
    /// <param name="throwWeap">The throwable grenade item to throw quickly.</param>
    /// <param name="callback">Callback invoked upon completion.</param>
    /// <param name="scheduled">Whether the transition is scheduled.</param>
    public override void Proceed(ThrowWeap throwWeap, Callback<IQuickGrenadeThrowController> callback, bool scheduled = true)
    {
        HandsControllerFactory factory = new(this, throwWeap);
        Func<QuickGrenadeThrowHandsController> func = new(factory.CreateObservedQuickGrenadeController);
        new Process<QuickGrenadeThrowHandsController, IQuickGrenadeThrowController>(this, func, throwWeap, false)
            .Proceed(null, callback, scheduled);
    }

    /// <summary>
    /// Transitions hands to a firearm weapon controller.
    /// </summary>
    /// <param name="weapon">The weapon item to equip.</param>
    /// <param name="callback">Callback invoked upon completion.</param>
    /// <param name="scheduled">Whether the transition is scheduled.</param>
    public override void Proceed(Weapon weapon, Callback<IFirearmHandsController> callback, bool scheduled = true)
    {
        HandsControllerFactory factory = new(this, weapon);
        Func<FirearmController> func = new(factory.CreateObservedFirearmController);
        new Process<FirearmController, IFirearmHandsController>(this, func, factory.Item, true)
            .Proceed(null, callback, scheduled);
    }

    /// <summary>
    /// Transitions hands to a medical item controller.
    /// </summary>
    /// <param name="meds">The medical item to use.</param>
    /// <param name="bodyParts">Body parts to treat.</param>
    /// <param name="callback">Callback invoked upon completion.</param>
    /// <param name="animationVariant">Animation variant index.</param>
    /// <param name="scheduled">Whether the transition is scheduled.</param>
    public override void Proceed(Meds meds, OneAndList<EBodyPart> bodyParts, Callback<IMedsController> callback, int animationVariant, bool scheduled = true)
    {
        HandsControllerFactory factory = new(this)
        {
            MedsItem = meds,
            BodyParts = bodyParts,
            AnimationVariant = animationVariant
        };
        Func<MedsController> func = new(factory.CreateObservedMedsController);
        new Process<MedsController, IMedsController>(this, func, meds, false)
            .Proceed(null, callback, scheduled);
    }

    /// <summary>
    /// Transitions hands to a consumable food or drink controller.
    /// </summary>
    /// <param name="foodDrink">The food or drink item to consume.</param>
    /// <param name="amount">Amount of the item to consume.</param>
    /// <param name="callback">Callback invoked upon completion.</param>
    /// <param name="animationVariant">Animation variant index.</param>
    /// <param name="scheduled">Whether the transition is scheduled.</param>
    public override void Proceed(FoodDrink foodDrink, float amount, Callback<IMedsController> callback, int animationVariant, bool scheduled = true)
    {
        HandsControllerFactory factory = new(this)
        {
            FoodItem = foodDrink,
            Amount = amount,
            AnimationVariant = animationVariant
        };
        Func<MedsController> func = new(factory.CreateObservedMedsController);
        new Process<MedsController, IMedsController>(this, func, foodDrink, false)
            .Proceed(null, callback, scheduled);
    }
    #endregion

    /// <summary>
    /// Overridden to suppress camera FOV change adjustments on observed players.
    /// </summary>
    /// <param name="fov">New field of view angle.</param>
    public override void OnFovUpdatedEvent(int fov)
    {
        // Do nothing
    }

    /// <summary>
    /// Overridden to suppress greeting notifications on observed players.
    /// </summary>
    /// <param name="sender">Sender nickname.</param>
    public override void ShowHelloNotification(string sender)
    {
        // Do nothing
    }

    /// <summary>
    /// Overridden to suppress local health controller tick on observed players.
    /// </summary>
    /// <param name="deltaTime">Frame delta time.</param>
    public override void HealthControllerUpdate(float deltaTime)
    {
        // Do nothing
    }

    /// <summary>
    /// Plays the vocal audio clip for a voice phrase unless running in headless mode.
    /// </summary>
    /// <param name="event">Phrase trigger type.</param>
    /// <param name="clip">The audio clip to play.</param>
    /// <param name="bank">Audio tag bank containing the clip.</param>
    /// <param name="speaker">The speaker initiating playback.</param>
    public override void OnPhraseTold(EPhraseTrigger @event, TaggedClip clip, TagBank bank, BaseSpeaker speaker)
    {
        if (!FikaBackendUtils.IsHeadless)
        {
            PlayPhraseClip(clip);
        }
    }

    /// <summary>
    /// Invokes the rotation action delegate on the movement context to update body alignment.
    /// </summary>
    /// <param name="forceApplyToOriginalRibcage">Whether to force apply rotation directly to ribcage transform.</param>
    public override void MouseLook(bool forceApplyToOriginalRibcage = false)
    {
        MovementContext.RotationAction?.Invoke(this);
    }

    /// <summary>
    /// Checks the ground surface beneath the player to update movement audio and surface physics data.
    /// </summary>
    /// <param name="range">Surface check detection range.</param>
    /// <returns><see langword="true"/> if within hearing range; otherwise, <see langword="false"/>.</returns>
    public override bool CheckSurface(float range)
    {
        if (_lastDistance > (range * ProtagonistHearing))
        {
            return false;
        }

        (var hit, var surfaceSound) = CalculateMovementSurface();
        UpdateSurfaceData(hit, surfaceSound);
        if (Environment == EnvironmentType.Outdoor)
        {
            method_35();
        }
        return true;
    }

    /// <summary>
    /// Samples interpolated and extrapolated snapshot states from the network and applies positions, rotations, animators, and sound cues.
    /// </summary>
    /// <param name="localTime">Current local network time used for snapshot interpolation.</param>
    public void ManualStateUpdate(double localTime)
    {
        var bufferState = Snapshotter.GetInterpolationIndices(localTime, out var from, out var to, out var t);

        if (bufferState == EBufferState.Stale)
        {
            if (!CurrentPlayerState.IsMoving)
            {
                return;
            }

            CurrentPlayerState.Velocity = Vector3.zero;
            CurrentPlayerState.MovementDirection = Vector2.zero;
            CurrentPlayerState.IsMoving = false;
            ObservedCharacterController._velocity = CurrentPlayerState.Velocity;

            MovementContext.PlayerAnimatorEnableInert(CurrentPlayerState.IsMoving);
            MovementContext.MovementDirection = CurrentPlayerState.MovementDirection;
            return;
        }

        ref readonly var snapFrom = ref Snapshotter.GetSnapshot(from);
        var currentState = CurrentPlayerState;

        if (bufferState == EBufferState.Interpolating)
        {
            ref readonly var snapTo = ref Snapshotter.GetSnapshot(to);

            currentState.Rotation = new Vector2(
                Mathf.LerpAngle(snapFrom.Data.Rotation.x, snapTo.Data.Rotation.x, t),
                Mathf.LerpUnclamped(snapFrom.Data.Rotation.y, snapTo.Data.Rotation.y, t)
            );

            currentState.HeadRotation = Vector3.LerpUnclamped(snapFrom.Data.HeadRotation, snapTo.Data.HeadRotation, t);
            currentState.Position = Vector3.LerpUnclamped(snapFrom.Data.Position, snapTo.Data.Position, t);

            var newDir = currentState.MovementDirection = Vector2.LerpUnclamped(snapFrom.Data.MovementDirection, snapTo.Data.MovementDirection, t);
            if (!_isZombie && (snapTo.Data.State is EPlayerState.Idle or EPlayerState.Transition || newDir.sqrMagnitude < _movementDeadZoneSqr))
            {
                currentState.MovementDirection = Vector2.zero;
                currentState.IsMoving = false;
            }
            else
            {
                currentState.MovementDirection = newDir;
                currentState.IsMoving = true;
            }

            currentState.State = snapTo.Data.State;
            currentState.Tilt = Mathf.LerpUnclamped(snapFrom.Data.Tilt, snapTo.Data.Tilt, t);
            currentState.Step = snapTo.Data.Step;
            currentState.MovementSpeed = Mathf.LerpUnclamped(snapFrom.Data.MovementSpeed, snapTo.Data.MovementSpeed, t);
            currentState.SprintSpeed = Mathf.LerpUnclamped(snapFrom.Data.SprintSpeed, snapTo.Data.SprintSpeed, t);
            currentState.IsProne = snapTo.Data.IsProne;
            currentState.PoseLevel = Mathf.LerpUnclamped(snapFrom.Data.PoseLevel, snapTo.Data.PoseLevel, t);
            currentState.IsSprinting = snapTo.Data.IsSprinting;
            currentState.Stamina = snapTo.Data.Physical;
            currentState.Blindfire = snapTo.Data.Blindfire;
            currentState.WeaponOverlap = Mathf.LerpUnclamped(snapFrom.Data.WeaponOverlap, snapTo.Data.WeaponOverlap, t);
            currentState.LeftStanceDisabled = snapTo.Data.LeftStanceDisabled;
            currentState.IsGrounded = snapTo.Data.IsGrounded;

            var velocity = Vector3.LerpUnclamped(snapFrom.Data.Velocity, snapTo.Data.Velocity, t);
            if (velocity.sqrMagnitude < _velocityDeadZoneSqr)
            {
                velocity = Vector3.zero;
            }

            currentState.Velocity = velocity;
        }
        else if (bufferState == EBufferState.Extrapolating)
        {
            currentState.Position = snapFrom.Data.Position + (snapFrom.Data.Velocity * t);

            currentState.Rotation = snapFrom.Data.Rotation;
            currentState.HeadRotation = snapFrom.Data.HeadRotation;
            currentState.MovementDirection = snapFrom.Data.MovementDirection;
            currentState.IsMoving = snapFrom.Data.MovementDirection.sqrMagnitude > _movementDeadZoneSqr;

            currentState.State = snapFrom.Data.State;
            currentState.Tilt = snapFrom.Data.Tilt;
            currentState.Step = snapFrom.Data.Step;
            currentState.MovementSpeed = snapFrom.Data.MovementSpeed;
            currentState.SprintSpeed = snapFrom.Data.SprintSpeed;
            currentState.IsProne = snapFrom.Data.IsProne;
            currentState.PoseLevel = snapFrom.Data.PoseLevel;
            currentState.IsSprinting = snapFrom.Data.IsSprinting;
            currentState.Stamina = snapFrom.Data.Physical;
            currentState.Blindfire = snapFrom.Data.Blindfire;
            currentState.WeaponOverlap = snapFrom.Data.WeaponOverlap;
            currentState.LeftStanceDisabled = snapFrom.Data.LeftStanceDisabled;
            currentState.IsGrounded = snapFrom.Data.IsGrounded;
            currentState.Velocity = snapFrom.Data.Velocity;
        }

        if (!botPlayerCulling.IsVisible)
        {
            Position = CurrentPlayerState.Position;
            Rotation = CurrentPlayerState.Rotation;
            ObservedCharacterController._velocity = CurrentPlayerState.Velocity;

            if (!_isServer)
            {
                return;
            }

            if (CurrentPlayerState.State == EPlayerState.Jump)
            {
                MovementContext.EmitJumpNoise(1f);
                return;
            }

            if (CurrentPlayerState.IsMoving)
            {
                MovementContext.EmitStepNoise(CurrentPlayerState.MovementDirection);
            }

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
                MovementContext.EmitJumpNoise(1f);
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
            MovementContext.EmitStepNoise(CurrentPlayerState.MovementDirection);
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

        ObservedCharacterController._velocity = CurrentPlayerState.Velocity;
    }

    /// <summary>
    /// Casts an interaction ray forward to detect interactable players in front of this observed player.
    /// </summary>
    public override void InteractionRaycast()
    {
        if (_playerLookRaycastTransform == null || !HealthController.IsAlive)
        {
            return;
        }

        InteractableObjectIsProxy = false;
        var interactionRay = InteractionRay;
        Sense = false;
        var gameObject = GameWorld.FindInteractable(interactionRay, out _);
        if (gameObject != null)
        {
            if (gameObject.TryGetComponent<Player>(out var otherPlayer) && InteractablePlayer != otherPlayer)
            {
                InteractablePlayer = (otherPlayer != this) ? otherPlayer : null;
            }
            return;
        }

        InteractablePlayer = null;
    }

    /// <summary>
    /// Spawns and configures an <see cref="ObservedCorpse"/> or standard <see cref="Corpse"/> with replicated inventory upon death.
    /// </summary>
    /// <returns>The created corpse instance.</returns>
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

    /// <summary>
    /// Creates the nested audio source for environmental sound effects if not in headless mode.
    /// </summary>
    public override void CreateNestedSource()
    {
        if (!FikaBackendUtils.IsHeadless)
        {
            base.CreateNestedSource();
        }
    }

    /// <summary>
    /// Creates the speech audio source for voice phrases if not in headless mode.
    /// </summary>
    public override void CreateSpeechSource()
    {
        if (!FikaBackendUtils.IsHeadless)
        {
            base.CreateSpeechSource();
        }
    }

    /// <summary>
    /// Handles death events, cleanups, corpse spawning, UI notifications, and network unregistration.
    /// </summary>
    /// <param name="damageType">Lethal damage type that killed the player.</param>
    public override void OnDead(EDamageType damageType)
    {
        ClearReviveInteractable();

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
                    NotificationManager.DisplayWarningNotification(string.Format(LocaleUtils.GROUP_MEMBER_DIED_FROM.Localized(),
                        [ColorizeText(EColor.GREEN, nickname), ColorizeText(EColor.RED, ("DamageType_" + damageType.ToString()).Localized())]));
                }
                else
                {
                    NotificationManager.DisplayWarningNotification(string.Format(LocaleUtils.GROUP_MEMBER_DIED.Localized(),
                        ColorizeText(EColor.GREEN, nickname)));
                }
            }
            if (LocaleUtils.IsBoss(Profile.Info.Settings.Role, out var name) && IsObservedAI && LastAggressor != null && LastAggressor is FikaPlayer aggressor)
            {
                var aggressorNickname = !string.IsNullOrEmpty(LastAggressor.Profile.Info.MainProfileNickname) ? LastAggressor.Profile.Info.MainProfileNickname : LastAggressor.Profile.Nickname;
                if (aggressor.gameObject.name.StartsWith("Player_") || aggressor.IsYourPlayer)
                {
                    NotificationManager.DisplayMessageNotification(string.Format(LocaleUtils.KILLED_BOSS.Localized(),
                    [ColorizeText(EColor.GREEN, LastAggressor.Profile.Info.MainProfileNickname), ColorizeText(EColor.BROWN, name)]),
                    iconType: EFT.Communications.ENotificationIconType.Friend);
                }
            }
        }
        Singleton<BetterAudio>.Instance.ProtagonistHearingChanged -= UpdateSoundRolloff;
        base.OnDead(damageType);
        if (botPlayerCulling != null)
        {
            botPlayerCulling.DisableCullingOnDead();
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

    /// <summary>
    /// Overridden to suppress transit interactions on observed players.
    /// </summary>
    /// <param name="controller">The transit controller.</param>
    /// <param name="transitPointId">Transit point ID.</param>
    /// <param name="keyId">Key ID used for transit.</param>
    /// <param name="time">In-game transition time.</param>
    public override void TransitInteraction(TransitController controller, int transitPointId, string keyId, EDateTime time)
    {
        // Do nothing
    }

    /// <summary>
    /// Deserializes and processes incoming damage packet information, updating damage history and inflicting damage.
    /// </summary>
    /// <param name="packet">Network damage packet containing hit details.</param>
    public override void HandleDamagePacket(DamagePacket packet)
    {
        DamageInfo damageInfo = new()
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

    /// <summary>
    /// Handles kill notifications, shared quest progression, and experience distribution when killed by a group member.
    /// </summary>
    /// <param name="aggressor">The player that inflicted lethal damage.</param>
    /// <param name="damageInfo">Lethal damage details.</param>
    /// <param name="bodyPart">Fatal body part.</param>
    /// <param name="lethalDamageType">Fatal damage type.</param>
    public override void OnBeenKilledByAggressor(IPlayer aggressor, DamageInfo damageInfo, EBodyPart bodyPart, EDamageType lethalDamageType)
    {
        // only handle if it was ourselves as otherwise it's irrelevant
        if (LastAggressor.IsYourPlayer)
        {
            base.OnBeenKilledByAggressor(aggressor, damageInfo, bodyPart, lethalDamageType);
            return;
        }

        if (string.Equals(aggressor.GroupId, FikaGlobals.FikaGroupId, StringComparison.Ordinal) && !aggressor.IsYourPlayer)
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

            if (FikaPlugin.Instance.Settings.SharedQuestProgression && FikaPlugin.Instance.Settings.EasyKillConditions.Value)
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

    /// <summary>
    /// Overridden to suppress external interactions on observed players.
    /// </summary>
    public override void ExternalInteraction()
    {
        // Do nothing
    }

    /// <summary>
    /// Toggles the downed state, spawning or destroying revive interactables, triggering agony phrases, and updating UI.
    /// </summary>
    /// <param name="downed"><see langword="true"/> if entering downed state; otherwise, <see langword="false"/>.</param>
    public override void ToggleDowned(bool downed)
    {
#if DEBUG
        FikaGlobals.LogInfo($"Setting {Profile.GetCorrectedNickname()} downed state to {downed}");
#endif
        Downed = downed;
        NetworkHealthController.IsAlive = !Downed;
        if (_healthBar != null)
        {
            _healthBar.ToggleDowned(downed);
        }

        if (downed)
        {
            if (FikaPlugin.Instance.Settings.ShowNotifications.Value)
            {
                NotificationManager.DisplayWarningNotification(string.Format(LocaleUtils.UI_REVIVING_BEEN_DOWNED.Localized(),
                                ColorizeText(EColor.GREEN, Profile.GetCorrectedNickname())));
            }
            Speaker.Play(EPhraseTrigger.OnAgony, HealthStatus, true);
            if (_reviveInteractable != null)
            {
                FikaGlobals.LogWarning($"ReviveInteractable was not null on {Profile.GetCorrectedNickname()}");
                ClearReviveInteractable();
            }

            _reviveInteractable = ReviveInteractable.Create(this);
            return;
        }

        if (FikaPlugin.Instance.Settings.ShowNotifications.Value)
        {
            NotificationManager.DisplayWarningNotification(string.Format(LocaleUtils.UI_REVIVING_BEEN_REVIVED.Localized(),
                            ColorizeText(EColor.GREEN, Profile.GetCorrectedNickname())));
        }

        if (_reviveInteractable == null)
        {
#if DEBUG
            FikaGlobals.LogWarning("ReviveInteractable was null, this is intentional if we revived");
#endif
            return;
        }

        _reviveInteractable.RemoveRagdoll();
        ClearReviveInteractable();
    }

    /// <summary>
    /// Updates the reviving status on the revive interactable and health bar when a teammate revives this player.
    /// </summary>
    /// <param name="reviving">Whether revival is currently in progress.</param>
    /// <param name="nickname">Nickname of the player performing the revival.</param>
    public override void ToggleRevive(bool reviving, string nickname)
    {
#if DEBUG
        FikaGlobals.LogInfo($"{Profile.GetCorrectedNickname()} is being revived by {nickname}");
#endif
        if (_reviveInteractable != null)
        {
            _reviveInteractable.BeingRevived = reviving;
        }
        if (_healthBar != null)
        {
            _healthBar.ToggleRevive(reviving, nickname);
        }
    }

    /// <summary>
    /// Destroys and cleans up the active <see cref="ReviveInteractable"/> component.
    /// </summary>
    internal void ClearReviveInteractable()
    {
        var interactable = _reviveInteractable;
        if (interactable != null)
        {
            Destroy(interactable);
        }
        _reviveInteractable = null;
    }

    /// <summary>
    /// Loads and attaches the third-person compass model to the player's ribcage transform.
    /// </summary>
    internal void CreateObservedCompass()
    {
        const string bundlePath = "assets/content/weapons/additional_hands/item_compass.bundle";
        if (!_compassInstantiated)
        {
            var transform = Singleton<ObjectsFactory>.Instance.CreateFromPool<Transform>(new ResourceKey
            {
                path = bundlePath
            });
            transform.SetParent(PlayerBones.Ribcage.Original, false);
            transform.localRotation = Quaternion.identity;
            transform.localPosition = Vector3.zero;
            UpdateCompassController(transform.gameObject);
            _compassInstantiated = true;
        }
    }

    /// <summary>
    /// Deserializes and equips the given inventory descriptor onto this observed player, refreshing slot views.
    /// </summary>
    /// <param name="inventoryDescriptor">Serialized item descriptor containing equipment hierarchy.</param>
    public void SetInventory(ItemDescriptor inventoryDescriptor)
    {
        if (HandsController != null)
        {
            HandsController.FastForwardCurrentState();
        }

        var inventory = new InventoryDescriptor()
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

    /// <summary>
    /// Rebuilds visual third-person slot views for all equipped items, holsters, and weapon bones.
    /// </summary>
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
                    Dictionary<string, ContainerCollectionView.SlotView> currentViews = [];
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
                    foreach (IContainer container in newSlots)
                    {
                        if (container is Slot slot)
                        {
                            if (slot.ContainedItem == null)
                            {
                                var transform = TransformTools.FindTransformRecursive(controller.CCV.GameObject.transform,
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

    /// <summary>
    /// Applies replicated vaulting or climbing animation parameters and trajectory offsets.
    /// </summary>
    /// <param name="packet">Vaulting packet containing speed, height, and trajectory details.</param>
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

    /// <summary>
    /// Configures packet senders, health bar creation, vaulting components, and spawns team notifications.
    /// </summary>
    public void InitObservedPlayer()
    {
        PacketSender = gameObject.AddComponent<ObservedPacketSender>();

        if (IsObservedAI)
        {
            BotStatePacket packet = new()
            {
                NetId = NetId,
                Type = BotStatePacket.EStateType.LoadBot
            };

            PacketSender.NetworkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered);

            if (_vaultingComponent != null)
            {
                UpdateEvent -= _vaultingComponent.DoVaultingTick;
            }

            _vaultingComponent = null;
            _vaultingComponentDebug = null;
            _vaultingParameters = null;
            _vaultingGameplayRestrictions = null;
            _vaultAudioController = null;
            _sprintVaultAudioController = null;
            _climbAudioController = null;
        }

        if (!IsObservedAI)
        {
            Profile.Info.GroupId = "Fika";
            Profile.Info.TeamId = "Fika";
            if (!FikaBackendUtils.IsHeadless)
            {
                CreateHealthBarAsync(destroyCancellationToken)
                    .Forget();
            }

            if (_vaultingComponent != null)
            {
                UpdateEvent -= _vaultingComponent.DoVaultingTick;
            }
            _vaultingComponent = null;
            _vaultingComponentDebug = null;
            _vaultingParameters = null;
            _vaultingGameplayRestrictions = null;

            InitVaultingAudioControllers(_observedVaultingParameters);

            if (FikaPlugin.Instance.Settings.ShowNotifications.Value)
            {
                NotificationManager.DisplayMessageNotification(string.Format(LocaleUtils.GROUP_MEMBER_SPAWNED.Localized(),
                    ColorizeText(EColor.GREEN, Profile.Info.MainProfileNickname)),
                ENotificationDurationType.Default, ENotificationIconType.Friend);
            }

            if (Profile.Side is not EPlayerSide.Savage)
            {
                var dogtag = Equipment.GetSlot(EquipmentSlot.Dogtag).ContainedItem;
                if (dogtag != null)
                {
                    var result = ItemManipulator.Discard(dogtag, InventoryController);
                }
            }
        }
    }

    /// <summary>
    /// Asynchronously waits for the game to start and instantiates the floating nameplate/health bar.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to abort waiting.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task CreateHealthBarAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var fikaGame = Singleton<IFikaGame>.Instance;
            if (fikaGame == null)
            {
                return;
            }

            while (fikaGame.GameController.GameInstance.Status != GameStatus.Started)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
            }

            if (FikaPlugin.Instance.Settings.AllowNamePlates)
            {
                _healthBar = FikaHealthBar.Create(this);
            }

            while (Singleton<GameWorld>.Instance.MainPlayer == null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
            }

            Singleton<GameWorld>.Instance.MainPlayer.StatisticsManager.OnGroupMemberConnected(Inventory);
        }
        catch (OperationCanceledException)
        {

        }
        catch (Exception ex)
        {
            FikaGlobals.LogError($"Error in CreateHealthBarAsync: {ex}");
        }
    }

    /// <summary>
    /// Executes late update processing including procedural animations, IK passes, prop updates, and corpse culling.
    /// </summary>
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

    /// <summary>
    /// Overridden to suppress local muffled state updates.
    /// </summary>
    public override void UpdateMuffledState()
    {
        // Do nothing
    }

    /// <summary>
    /// Overridden to suppress voice muffled state packets from observed players.
    /// </summary>
    /// <param name="isMuffled">Whether the voice is muffled.</param>
    public override void SendVoiceMuffledState(bool isMuffled)
    {
        // Do nothing
    }

    /// <summary>
    /// Configures audio mixer groups on speech and VOIP sources according to muffled status.
    /// </summary>
    /// <param name="muffled"><see langword="true"/> to route through occluded mixer; otherwise, <see langword="false"/>.</param>
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

    /// <summary>
    /// Overridden to suppress landing adjustments on observed players.
    /// </summary>
    /// <param name="d">Adjustment delta.</param>
    public override void LandingAdjustments(float d)
    {
        // Do nothing
    }

    /// <summary>
    /// Handles animated gestures such as friendly wave greetings towards looked-at players.
    /// </summary>
    /// <param name="interaction">Interaction gesture type.</param>
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

    /// <summary>
    /// Pauses all active health effects on this observed player.
    /// </summary>
    public override void PauseAllEffectsOnPlayer()
    {
        NetworkHealthController.PauseAllEffects();
    }

    /// <summary>
    /// Unpauses all active health effects on this observed player.
    /// </summary>
    public override void UnpauseAllEffectsOnPlayer()
    {
        NetworkHealthController.UnpauseAllEffects();
    }

    /// <summary>
    /// Overridden to suppress vaulting triggers on observed players.
    /// </summary>
    public override void OnVaulting()
    {
        // Do nothing
    }

    /// <summary>
    /// Immediately succeeds the callback when replacing an active controller.
    /// </summary>
    /// <param name="removingItem">Item being removed from hands.</param>
    /// <param name="callback">Completion callback.</param>
    public override void SetControllerInsteadRemovedOne(Item removingItem, Callback callback)
    {
        callback.Succeed();
    }

    /// <summary>
    /// Main per-frame update loop for observed players, handling movement updates, culling, and state-specific logic.
    /// </summary>
    /// <param name="deltaTime">Frame delta time.</param>
    /// <param name="platformDeltaTime">Platform-specific delta time, if any.</param>
    /// <param name="loop">Update loop index.</param>
    public override void ManualUpdate(float deltaTime, float? platformDeltaTime = null, int loop = 1)
    {
        MovementUpdate(deltaTime);

        if (HealthController.IsAlive)
        {
            if (Time.frameCount % 2 == _frameSkip)
            {
                UpdateTriggerColliderSearcher(deltaTime, botPlayerCulling.IsCloseToMyPlayerCamera);
            }
            botPlayerCulling.ManualUpdate(deltaTime);
            switch (_currentState)
            {
                case EPlayerState.Idle:
                    TickIdleState();
                    break;

                case EPlayerState.Run:
                case EPlayerState.MoveZombieState:
                case EPlayerState.StartMoveZombieState:
                case EPlayerState.EndMoveZombieState:
                    TickRunState();
                    break;

                case EPlayerState.Sprint:
                    TickSprintState();
                    break;
            }
        }
    }

    /// <summary>
    /// Initializes audio settings and hooks into protagonist hearing sensitivity changes.
    /// </summary>
    public override void InitAudioController()
    {
        if (!FikaBackendUtils.IsHeadless)
        {
            base.InitAudioController();
            Singleton<BetterAudio>.Instance.ProtagonistHearingChanged += UpdateSoundRolloff;
        }
    }

    /// <summary>
    /// Recalculates movement sound multipliers and voice rolloff distance.
    /// </summary>
    private void UpdateSoundRolloff()
    {
        CalculateMovementVolumeDefaultMultiplier(CommonAssets.Scripts.Audio.EAudioMovementState.Run);
        UpdateVoiceSoundRolloff();
    }

    /// <summary>
    /// Updates speech audio rolloff distance scaled by protagonist hearing.
    /// </summary>
    private void UpdateVoiceSoundRolloff()
    {
        SpeechSource?.SetRolloff(60f * ProtagonistHearing);
    }

    /// <summary>
    /// Starts interaction with a world interactive object (e.g. opening a door).
    /// </summary>
    /// <param name="interactiveObject">The interactive world object.</param>
    /// <param name="interactionResult">Interaction result parameters.</param>
    /// <param name="callback">Action callback on start.</param>
    public override void StartInteraction(WorldInteractiveObject interactiveObject, InteractionResult interactionResult, Action callback)
    {
        CurrentManagedState.StartDoorInteraction(interactiveObject, interactionResult, callback);
        UpdateInteractionCast();
    }

    /// <summary>
    /// Executes interaction on a world interactive door or object.
    /// </summary>
    /// <param name="door">The door being operated.</param>
    /// <param name="interactionResult">Interaction parameters.</param>
    public override void ExecuteInteraction(WorldInteractiveObject door, InteractionResult interactionResult)
    {
        if (door != null)
        {
            CurrentManagedState.ExecuteDoorInteraction(door, interactionResult, null, this);
        }
    }

    /// <summary>
    /// Gets whether grenade animations should update according to point of view (always <see langword="true"/>).
    /// </summary>
    /// <returns>Always <see langword="true"/>.</returns>
    public override bool UpdateGrenadeAnimatorDuePoV()
    {
        return true;
    }

    /// <summary>
    /// Overridden to suppress physics fixed update ticks on observed players.
    /// </summary>
    public override void FixedUpdateTick()
    {
        // Do nothing
    }

    /// <summary>
    /// Cleans up revive interactables, culling objects, hands controllers, health bars, and network listeners when destroyed.
    /// </summary>
    public override void OnDestroy()
    {
        ClearReviveInteractable();
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
        if (_healthBar != null)
        {
            Destroy(_healthBar);
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
        if (HealthController.IsAlive && !Singleton<IFikaNetworkManager>.Instance.ObservedPlayers.Remove(this) && !Profile.Nickname.StartsWith("headless_"))
        {
            FikaGlobals.LogWarning($"Failed to remove {ProfileId}, {Profile.Nickname} from observed list");
        }
        base.OnDestroy();
    }

    /// <summary>
    /// Resets blind fire settings when hands interaction state transitions.
    /// </summary>
    /// <param name="value">New hands interaction state.</param>
    /// <param name="animationId">Animation identifier.</param>
    public override void SendHandsInteractionStateChanged(bool value, int animationId)
    {
        if (value)
        {
            MovementContext.SetBlindFire(0);
        }
    }

    /// <summary>
    /// Handles an incoming proceed packet to equip or switch to the appropriate hands controller.
    /// </summary>
    /// <param name="packet">Network packet specifying the controller type and item.</param>
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

    /// <summary>
    /// Monitors an equipment slot and rebuilds third-person visual meshes when equipped items change.
    /// </summary>
    private sealed class ObservedSlotViewHandler : IDisposable
    {
        /// <summary>
        /// Monitored equipment slot.
        /// </summary>
        private readonly Slot _slot;

        /// <summary>
        /// Reference to the parent observed player.
        /// </summary>
        private readonly ObservedPlayer _observedPlayer;

        /// <summary>
        /// Equipment slot category type.
        /// </summary>
        private readonly EquipmentSlot _slotType;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObservedSlotViewHandler"/> class and subscribes to item changes.
        /// </summary>
        /// <param name="itemSlot">The inventory slot to monitor.</param>
        /// <param name="player">The parent observed player.</param>
        /// <param name="equipmentType">The equipment slot type.</param>
        public ObservedSlotViewHandler(Slot itemSlot, ObservedPlayer player, EquipmentSlot equipmentType)
        {
            _slot = itemSlot;
            _observedPlayer = player;
            _slotType = equipmentType;

            itemSlot.OnAddOrRemoveItem += HandleItemMove;
        }

        /// <summary>
        /// Unsubscribes from slot item events.
        /// </summary>
        public void Dispose()
        {
            _slot.OnAddOrRemoveItem -= HandleItemMove;
        }

        /// <summary>
        /// Reconstructs the slot view when an item is added or removed, notifying global equipment events.
        /// </summary>
        /// <param name="item">The item that changed.</param>
        private void HandleItemMove(Item item)
        {
            var slotBone = _observedPlayer.PlayerBody.GetSlotBone(_slotType);
            var alternativeHolsterBone = _observedPlayer.PlayerBody.GetAlternativeHolsterBone(_slotType);
            PlayerBody.SlotView newSlotView = new(_observedPlayer.PlayerBody, _slot, slotBone, _slotType,
                    _observedPlayer.Inventory.Equipment.GetSlot(EquipmentSlot.Backpack), alternativeHolsterBone, false);
            var oldSlotView = _observedPlayer.PlayerBody.SlotViews.AddOrReplace(_slotType, newSlotView);
            if (oldSlotView != null)
            {
                ClearSlotView(oldSlotView);
                oldSlotView.Dispose();
            }
            _observedPlayer.PlayerBody.ValidateHoodedDress(_slotType);
            GlobalEventsController.Instance.CreateCommonEvent<ObservedPlayerChangedEquipEvent>().Invoke(_observedPlayer.ProfileId);
            Dispose();
        }

        /// <summary>
        /// Resets force rendering flags on all renderers associated with the previous slot view.
        /// </summary>
        /// <param name="oldSlotView">The previous slot view being cleared.</param>
        private void ClearSlotView(PlayerBody.SlotView oldSlotView)
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

    /// <summary>
    /// Processes weapon procedural animations, FBBIK solvers, and bone transformations during late update.
    /// </summary>
    /// <param name="deltaTime">Frame delta time.</param>
    /// <param name="ikUpdateInterval">Update interval for full-body inverse kinematics.</param>
    private void ObservedVisualPass(float deltaTime, int ikUpdateInterval)
    {
        if (CustomAnimationsAreProcessing || !botPlayerCulling.IsVisible || !HealthController.IsAlive)
        {
            return;
        }

        _lastDistance = CameraManager.Instance.Distance(Transform.position);
        var isVisibleOrClose = IsVisible && _lastDistance <= EFTHardSettings.Instance.CULL_GROUNDER;

        if (_armsupdated && isVisibleOrClose && !UsedSimplifiedSkeleton)
        {
            ProceduralWeaponAnimation.ProcessEffectors(deltaTime, 2, Motion, Velocity);
            var weaponRoot = ProceduralWeaponAnimation.HandsContainer.WeaponRootAnim;
            PlayerBones.Offset = weaponRoot.localPosition;
            PlayerBones.DeltaRotation = weaponRoot.localRotation;
        }

        if (isVisibleOrClose && !UsedSimplifiedSkeleton)
        {
            RestoreIKPos();
            ObservedFBBIKUpdate(_lastDistance, ikUpdateInterval);
            MouseLook(false);
            const float num2 = 1f;
            var num4 = GetCurveValue(PlayerAnimator.LEFT_STANCE_CURVE);
            ProceduralWeaponAnimation.GetLeftStanceCurrentCurveValue(num4);
            _rightHand = 1f - (GetCurveValue(PlayerAnimator.RIGHT_HAND_WEIGHT) * num2);
            _leftHand = 1f - (GetCurveValue(PlayerAnimator.LEFT_HAND_WEIGHT) * num2);
            ThirdPersonWeaponRootAuthority = MovementContext.IsInMountedState ? 0f : (GetCurveValue(PlayerAnimator.WEAPON_ROOT_3RD) * num2);
            AdjustUtilityLayerWeight(_lastDistance);
            if (_armsupdated)
            {
                var num5 = ThirdPersonWeaponRootAuthority;
                if (MovementContext.StationaryWeapon != null)
                {
                    num5 = 0f;
                }
                PlayerBones.ShiftWeaponRoot(deltaTime, EPointOfView.ThirdPerson, num5);
            }
            PlayerBones.RotateHead(0f, ProceduralWeaponAnimation.GetHeadRotation(),
                MovementContext.LeftStanceEnabled && HasFirearmInHands(), num4,
                ProceduralWeaponAnimation.IsAiming);
            HandPosers[0].weight = _leftHand;
            _limbs[0].solver.IKRotationWeight = _limbs[0].solver.IKPositionWeight = _leftHand;
            _limbs[1].solver.IKRotationWeight = _limbs[1].solver.IKPositionWeight = _rightHand;
            IkProcess(_lastDistance);
            AdjustElbows(num2);
            IkApply(_lastDistance);
            if (_rightHand < 1f)
            {
                PlayerBones.Kinematics(_markers[1], _rightHand);
            }
            var num6 = GetCurveValue(PlayerAnimator.AIMING_LAYER_CURVE);
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
    /// <summary>
    /// Fast-forwards and destroys the existing hands controller and instantiates a new controller from the given factory.
    /// </summary>
    /// <param name="controllerFactory">Factory delegate creating the new controller instance.</param>
    /// <param name="item">Item held in hands, or <see langword="null"/>.</param>
    private void CreateHandsController(Func<AbstractHandsController> controllerFactory, Item item)
    {
        CreateHandsControllerHandler handler = new((item != null) ? BeginSetInHands(item) : null);

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

        SpawnController(controllerFactory(), handler.DisposeHandler);
        ((ISetInHandsHandler)this).OnSetInHands(new(item, CommandStatus.Succeed, InventoryController));
        _shouldCullController = _handsController is EmptyHandsController or KnifeController or UsableItemController;
    }

    /// <summary>
    /// Spawns the hands controller corresponding to the specified controller type and item ID.
    /// </summary>
    /// <param name="controllerType">Hands controller type to spawn.</param>
    /// <param name="itemId">Item MongoDB identifier.</param>
    /// <param name="isStationary">Whether the controller is for a stationary weapon.</param>
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

    /// <summary>
    /// Spawns and equips an empty hands controller.
    /// </summary>
    private void CreateEmptyHandsController()
    {
        CreateHandsController(ReturnEmptyHandsController, null);
    }

    /// <summary>
    /// Factory delegate returning a new <see cref="ObservedEmptyHandsController"/>.
    /// </summary>
    /// <returns>A new <see cref="ObservedEmptyHandsController"/> instance.</returns>
    private AbstractHandsController ReturnEmptyHandsController()
    {
        return ObservedEmptyHandsController.Create(this);
    }

    /// <summary>
    /// Factory delegate returning a new <see cref="ObservedEmptyHandsController"/> for proceed operations.
    /// </summary>
    /// <returns>A new <see cref="ObservedEmptyHandsController"/> instance.</returns>
    private ObservedEmptyHandsController ProceedEmptyHandsController()
    {
        return ObservedEmptyHandsController.Create(this);
    }

    /// <summary>
    /// Spawns and equips a firearm or stationary weapon hands controller.
    /// </summary>
    /// <param name="itemId">Item identifier of the firearm.</param>
    /// <param name="isStationary">Whether the weapon is a stationary mount.</param>
    /// <param name="initial">Whether this is the initial spawn setup.</param>
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

    /// <summary>
    /// Spawns and equips a grenade hands controller.
    /// </summary>
    /// <param name="itemId">Item identifier of the grenade.</param>
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
        if (handler.Item is ThrowWeap)
        {
            CreateHandsController(handler.ReturnController, handler.Item);
        }
        else
        {
            FikaGlobals.LogError($"CreateGrenadeController: Item was not of type GrenadeClass, was {handler.Item.GetType()}!");
        }
    }

    /// <summary>
    /// Spawns and equips a medical item hands controller.
    /// </summary>
    /// <param name="itemId">Item identifier of the medical item.</param>
    /// <param name="bodyParts">Body parts being treated.</param>
    /// <param name="amount">Amount of medicine applied.</param>
    /// <param name="animationVariant">Animation variant index.</param>
    private void CreateMedsController(MongoID itemId, OneAndList<EBodyPart> bodyParts, float amount, int animationVariant)
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

    /// <summary>
    /// Spawns and equips a melee knife hands controller.
    /// </summary>
    /// <param name="itemId">Item identifier of the knife.</param>
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

    /// <summary>
    /// Spawns and equips a quick grenade throw hands controller.
    /// </summary>
    /// <param name="itemId">Item identifier of the grenade to throw.</param>
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
        if (handler.tem is ThrowWeap)
        {
            CreateHandsController(handler.ReturnController, handler.tem);
        }
        else
        {
            FikaGlobals.LogError($"CreateQuickGrenadeController: Item was not of type GrenadeClass, was {handler.tem.GetType()}!");
        }
    }

    /// <summary>
    /// Spawns and equips a quick knife attack hands controller.
    /// </summary>
    /// <param name="itemId">Item identifier of the knife.</param>
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

    /// <summary>
    /// Spawns and equips a usable item hands controller.
    /// </summary>
    /// <param name="itemId">Item identifier of the usable item.</param>
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

    /// <summary>
    /// Spawns and equips a quick-use item hands controller.
    /// </summary>
    /// <param name="itemId">Item identifier of the item.</param>
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

    /// <summary>
    /// Sets killer, fatal body part, and weapon metadata when the player is eliminated.
    /// </summary>
    /// <param name="killerId">Profile ID of the killer, if known.</param>
    /// <param name="bodyPart">Fatal body part.</param>
    /// <param name="weaponId">Template/item ID of the weapon used.</param>
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

    /// <summary>
    /// Updates the Full Body Biped IK (FBBIK) solver with iteration count scaled by distance from camera.
    /// </summary>
    /// <param name="distance">Distance in meters to camera.</param>
    /// <param name="ikUpdateInterval">Interval between solver updates when quick mode is active.</param>
    private void ObservedFBBIKUpdate(float distance, int ikUpdateInterval)
    {
        _fbbik.solver.iterations = (int)Mathf.Clamp(15f / distance, 0f, 2f);

        if (!_fbbik.solver.Quick && Time.time > TurnOffFbbikAt)
        {
            _fbbik.solver.Quick = true;
        }

        if (!_fbbik.solver.Quick || (Time.frameCount + _frameSkip) % ikUpdateInterval == 0)
        {
            _fbbik.solver.Update();
        }
    }

    /// <summary>
    /// Handler managing removal callbacks when transitioning hands controllers.
    /// </summary>
    /// <param name="fikaPlayer">The target observed player.</param>
    /// <param name="callback">The callback to invoke.</param>
    private sealed class RemoveHandsControllerHandler(ObservedPlayer fikaPlayer, Callback callback)
    {
        /// <summary>
        /// Reference to the target observed player.
        /// </summary>
        private readonly ObservedPlayer _fikaPlayer = fikaPlayer;

        /// <summary>
        /// The callback invoked upon completion.
        /// </summary>
        private readonly Callback _callback = callback;

        /// <summary>
        /// Invoked when the hands removal operation completes.
        /// </summary>
        /// <param name="result">Result containing the newly equipped empty hands controller.</param>
        public void Handle(Result<IEmptyHandsController> result)
        {
            if (_fikaPlayer._removeFromHandsCallback == _callback)
            {
                _fikaPlayer._removeFromHandsCallback = null;
            }
            _callback.Invoke(result);
        }
    }

    /// <summary>
    /// Handler managing the inventory operation lifecycle for setting items in hands.
    /// </summary>
    /// <param name="setInHandsOperation">The active inventory operation.</param>
    private sealed class CreateHandsControllerHandler(InventoryOperation setInHandsOperation)
    {
        /// <summary>
        /// The underlying inventory operation.
        /// </summary>
        public readonly InventoryOperation SetInHandsOperation = setInHandsOperation;

        /// <summary>
        /// Disposes the underlying inventory operation.
        /// </summary>
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

    /// <summary>
    /// Factory handler for instantiating an <see cref="ObservedFirearmController"/>.
    /// </summary>
    /// <param name="fikaPlayer">The target observed player.</param>
    private sealed class CreateFirearmControllerHandler(ObservedPlayer fikaPlayer)
    {
        /// <summary>
        /// Reference to the target observed player.
        /// </summary>
        private readonly ObservedPlayer _fikaPlayer = fikaPlayer;

        /// <summary>
        /// The weapon item to equip.
        /// </summary>
        public Item item;

        /// <summary>
        /// Creates and returns the firearm controller instance.
        /// </summary>
        /// <returns>A new <see cref="ObservedFirearmController"/>.</returns>
        internal AbstractHandsController ReturnController()
        {
            return ObservedFirearmController.Create(_fikaPlayer, (Weapon)item);
        }
    }

    /// <summary>
    /// Factory handler for instantiating an <see cref="ObservedGrenadeController"/>.
    /// </summary>
    /// <param name="fikaPlayer">The target observed player.</param>
    private sealed class CreateGrenadeControllerHandler(ObservedPlayer fikaPlayer)
    {
        /// <summary>
        /// Reference to the target observed player.
        /// </summary>
        private readonly ObservedPlayer _fikaPlayer = fikaPlayer;

        /// <summary>
        /// The grenade item to equip.
        /// </summary>
        public Item Item;

        /// <summary>
        /// Creates and returns the grenade controller instance.
        /// </summary>
        /// <returns>A new <see cref="ObservedGrenadeController"/>.</returns>
        internal AbstractHandsController ReturnController()
        {
            return ObservedGrenadeController.Create(_fikaPlayer, (ThrowWeap)Item);
        }
    }

    /// <summary>
    /// Factory handler for instantiating an <see cref="ObservedMedsController"/>.
    /// </summary>
    /// <param name="fikaPlayer">The target observed player.</param>
    /// <param name="item">The medical item.</param>
    /// <param name="bodyParts">Body parts to treat.</param>
    /// <param name="amount">Amount of medicine used.</param>
    /// <param name="animationVariant">Animation variant index.</param>
    private sealed class CreateMedsControllerHandler(ObservedPlayer fikaPlayer, Item item, OneAndList<EBodyPart> bodyParts, float amount, int animationVariant)
    {
        /// <summary>
        /// Reference to the target observed player.
        /// </summary>
        private readonly ObservedPlayer _fikaPlayer = fikaPlayer;

        /// <summary>
        /// The medical item.
        /// </summary>
        public readonly Item Item = item;

        /// <summary>
        /// Target body parts to treat.
        /// </summary>
        private readonly OneAndList<EBodyPart> _bodyParts = bodyParts;

        /// <summary>
        /// Dosage amount applied.
        /// </summary>
        private readonly float _amount = amount;

        /// <summary>
        /// Animation variant index.
        /// </summary>
        private readonly int _animationVariant = animationVariant;

        /// <summary>
        /// Creates and returns the medical controller instance.
        /// </summary>
        /// <returns>A new <see cref="ObservedMedsController"/>.</returns>
        internal AbstractHandsController ReturnController()
        {
            return ObservedMedsController.Create(_fikaPlayer, Item, _bodyParts, _amount, _animationVariant);
        }
    }

    /// <summary>
    /// Factory handler for instantiating an <see cref="ObservedKnifeController"/>.
    /// </summary>
    /// <param name="fikaPlayer">The target observed player.</param>
    private sealed class CreateKnifeControllerHandler(ObservedPlayer fikaPlayer)
    {
        /// <summary>
        /// Reference to the target observed player.
        /// </summary>
        private readonly ObservedPlayer _fikaPlayer = fikaPlayer;

        /// <summary>
        /// The knife component to equip.
        /// </summary>
        public KnifeComponent Knife;

        /// <summary>
        /// Creates and returns the knife controller instance.
        /// </summary>
        /// <returns>A new <see cref="ObservedKnifeController"/>.</returns>
        internal AbstractHandsController ReturnController()
        {
            return ObservedKnifeController.Create(_fikaPlayer, Knife);
        }
    }

    /// <summary>
    /// Factory handler for instantiating an <see cref="ObservedQuickGrenadeController"/>.
    /// </summary>
    /// <param name="fikaPlayer">The target observed player.</param>
    private sealed class CreateQuickGrenadeControllerHandler(ObservedPlayer fikaPlayer)
    {
        /// <summary>
        /// Reference to the target observed player.
        /// </summary>
        private readonly ObservedPlayer _fikaPlayer = fikaPlayer;

        /// <summary>
        /// The grenade item to quickly throw.
        /// </summary>
        public Item tem;

        /// <summary>
        /// Creates and returns the quick grenade controller instance.
        /// </summary>
        /// <returns>A new <see cref="ObservedQuickGrenadeController"/>.</returns>
        internal AbstractHandsController ReturnController()
        {
            return ObservedQuickGrenadeController.Create(_fikaPlayer, (ThrowWeap)tem);
        }
    }

    /// <summary>
    /// Factory handler for instantiating an <see cref="ObservedQuickKnifeController"/>.
    /// </summary>
    /// <param name="fikaPlayer">The target observed player.</param>
    private sealed class CreateQuickKnifeControllerHandler(ObservedPlayer fikaPlayer)
    {
        /// <summary>
        /// Reference to the target observed player.
        /// </summary>
        private readonly ObservedPlayer _fikaPlayer = fikaPlayer;

        /// <summary>
        /// The knife component for the quick attack.
        /// </summary>
        public KnifeComponent Knife;

        /// <summary>
        /// Creates and returns the quick knife controller instance.
        /// </summary>
        /// <returns>A new <see cref="ObservedQuickKnifeController"/>.</returns>
        internal AbstractHandsController ReturnController()
        {
            return ObservedQuickKnifeController.Create(_fikaPlayer, Knife);
        }
    }

    /// <summary>
    /// Factory handler for instantiating a usable item controller.
    /// </summary>
    /// <param name="fikaPlayer">The target observed player.</param>
    /// <param name="item">The usable item.</param>
    private sealed class CreateUsableItemControllerHandler(ObservedPlayer fikaPlayer, Item item)
    {
        /// <summary>
        /// Reference to the target observed player.
        /// </summary>
        private readonly ObservedPlayer _fikaPlayer = fikaPlayer;

        /// <summary>
        /// The usable item.
        /// </summary>
        public readonly Item Item = item;

        /// <summary>
        /// Creates and returns the usable item controller instance.
        /// </summary>
        /// <returns>A new <see cref="UsableItemController"/>.</returns>
        internal AbstractHandsController ReturnController()
        {
            return UsableItemController.CreateController<UsableItemController>(_fikaPlayer, Item);
        }
    }

    /// <summary>
    /// Factory handler for instantiating an <see cref="ObservedQuickUseItemController"/>.
    /// </summary>
    /// <param name="fikaPlayer">The target observed player.</param>
    /// <param name="item">The quick-use item.</param>
    private sealed class CreateQuickUseItemControllerHandler(ObservedPlayer fikaPlayer, Item item)
    {
        /// <summary>
        /// Reference to the target observed player.
        /// </summary>
        private readonly ObservedPlayer _fikaPlayer = fikaPlayer;

        /// <summary>
        /// The quick-use item.
        /// </summary>
        public readonly Item Item = item;

        /// <summary>
        /// Creates and returns the quick use item controller instance.
        /// </summary>
        /// <returns>A new <see cref="ObservedQuickUseItemController"/>.</returns>
        internal AbstractHandsController ReturnController()
        {
            return ObservedQuickUseItemController.Create(_fikaPlayer, Item);
        }
    }
}

#endregion