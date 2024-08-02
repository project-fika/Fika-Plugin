// © 2024 Lacyway All Rights Reserved

using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.HealthSystem;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.Vaulting;
using Fika.Core.Coop.Custom;
using Fika.Core.Coop.Factories;
using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.ObservedClasses;
using Fika.Core.Coop.PacketHandlers;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using Fika.Core.Utils;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using static Fika.Core.Networking.FikaSerialization;
using static Fika.Core.Utils.ColorUtils;

namespace Fika.Core.Coop.Players
{
    /// <summary>
    /// Observed players are any other players in the world for a client, including bots.
    /// They are all being handled by the server that moves them through packets.
    /// As a host this is only other clients.
    /// </summary>
    public class ObservedCoopPlayer : CoopPlayer
    {
        #region Fields and Properties
        public CoopPlayer MainPlayer => (CoopPlayer)Singleton<GameWorld>.Instance.MainPlayer;
        private float observedFixedTime = 0f;
        private FikaHealthBar healthBar = null;
        private Coroutine waitForStartRoutine;
        private bool isServer;
        public NetworkHealthControllerAbstractClass NetworkHealthController
        {
            get => HealthController as NetworkHealthControllerAbstractClass;
        }
        private readonly ObservedVaultingParametersClass ObservedVaultingParameters = new();
        public override bool CanBeSnapped => false;
        public override EPointOfView PointOfView { get => EPointOfView.ThirdPerson; }
        public override AbstractHandsController HandsController
        {
            get => base.HandsController;
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

        public override float ProtagonistHearing => Mathf.Max(1f, Singleton<BetterAudio>.Instance.ProtagonistHearing + 1f);

        private FollowerCullingObject followerCullingObject;
        //private List<Renderer> cullingRenderers = new(256);
        private List<DisablerCullingObject> cullingObjects = new();
        private bool CollidingWithReporter
        {
            get
            {
                if (cullingObjects.Count > 0)
                {
                    for (int i = 0; i < cullingObjects.Count; i++)
                    {
                        if (cullingObjects[i].HasEntered)
                        {
                            return true;
                        }
                    }
                    return false;
                }
                return true;
            }
        }
        public override bool IsVisible
        {
            get
            {
                if (followerCullingObject != null)
                {
                    return followerCullingObject.IsVisible && CollidingWithReporter;
                }
                return OnScreen;
            }
            set
            {

            }
        }
        public override float SqrCameraDistance
        {
            get
            {
                if (followerCullingObject != null)
                {
                    return followerCullingObject.SqrCameraDistance;
                }
                return base.SqrCameraDistance;
            }
        }
        #endregion

        public static async Task<ObservedCoopPlayer> CreateObservedPlayer(int playerId, Vector3 position, Quaternion rotation,
            string layerName, string prefix, EPointOfView pointOfView, Profile profile, bool aiControl,
            EUpdateQueue updateQueue, EUpdateMode armsUpdateMode, EUpdateMode bodyUpdateMode,
            CharacterControllerSpawner.Mode characterControllerMode, Func<float> getSensitivity,
            Func<float> getAimingSensitivity, IViewFilter filter)
        {
            ObservedCoopPlayer player = Create<ObservedCoopPlayer>(ResourceKeyManagerAbstractClass.PLAYER_BUNDLE_NAME, playerId, position, updateQueue,
                armsUpdateMode, bodyUpdateMode, characterControllerMode, getSensitivity, getAimingSensitivity, prefix,
                aiControl);

            player.IsYourPlayer = false;

            InventoryControllerClass inventoryController = new ObservedInventoryController(player, profile, true);

            PlayerHealthController tempController = new(profile.Health, player, inventoryController, profile.Skills, aiControl);
            byte[] healthBytes = tempController.SerializeState();

            ObservedHealthController healthController = new(healthBytes, inventoryController, profile.Skills);

            CoopObservedStatisticsManager statisticsManager = new();

            await player.Init(rotation, layerName, pointOfView, profile, inventoryController, healthController,
                statisticsManager, null, null, filter, EVoipState.NotAvailable, aiControl, false);

            player._handsController = EmptyHandsController.smethod_5<EmptyHandsController>(player);
            player._handsController.Spawn(1f, delegate { });

            player.AIData = new AIData(null, player);

            player.AggressorFound = false;

            player._animators[0].enabled = true;
            player._armsUpdateQueue = EUpdateQueue.Update;

            player.isServer = FikaBackendUtils.IsServer;

            return player;
        }

        public override BasePhysicalClass CreatePhysical()
        {
            return new BasePhysicalClass();
        }

        public override bool CheckSurface()
        {
            float spreadRange = 42f * ProtagonistHearing;
            return !(Distance - spreadRange > 0);
        }

        public override void PlayGroundedSound(float fallHeight, float jumpHeight)
        {
            (bool hit, BaseBallistic.ESurfaceSound surfaceSound) values = method_57();
            method_58(values.hit, values.surfaceSound);
            base.PlayGroundedSound(fallHeight, jumpHeight);
        }

        public override void OnSkillLevelChanged(GClass1778 skill)
        {
            //base.OnSkillLevelChanged(skill);
        }

        public override void OnWeaponMastered(MasterSkillClass masterSkill)
        {
            //base.OnWeaponMastered(masterSkill);
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

        public override void UpdatePhones()
        {
            // Do nothing
        }

        public override void FaceshieldMarkOperation(FaceShieldComponent armor, bool hasServerOrigin)
        {
            // Do nothing
        }

        public override void SetAudioProtagonist()
        {
            // Do nothing
        }

        public override void ManageAggressor(DamageInfo damageInfo, EBodyPart bodyPart, EBodyPartColliderType colliderType)
        {
            if (_isDeadAlready)
            {
                return;
            }
            if (!HealthController.IsAlive)
            {
                _isDeadAlready = true;
            }
            Player player = (damageInfo.Player == null) ? null : Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(damageInfo.Player.iPlayer.ProfileId);
            if (player == this)
            {
                return;
            }
            if (player == null)
            {
                return;
            }
            if (damageInfo.Weapon != null)
            {
                player.ExecuteShotSkill(damageInfo.Weapon);
            }

            if (player.IsYourPlayer)
            {
                bool flag = damageInfo.DidBodyDamage / HealthController.GetBodyPartHealth(bodyPart, false).Maximum >= 0.6f && HealthController.FindExistingEffect<GInterface244>(bodyPart) != null;
                player.StatisticsManager.OnEnemyDamage(damageInfo, bodyPart, ProfileId, Side, Profile.Info.Settings.Role,
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
            return true;
        }

        public override void SendHeadlightsPacket(bool isSilent)
        {
            // Do nothing
        }

        public override void ApplyDamageInfo(DamageInfo damageInfo, EBodyPart bodyPartType, EBodyPartColliderType colliderType, float absorbed)
        {
            if (damageInfo.DamageType == EDamageType.Landmine && FikaBackendUtils.IsServer)
            {
                PacketSender.DamagePackets.Enqueue(new()
                {
                    Damage = damageInfo.Damage,
                    DamageType = damageInfo.DamageType,
                    BodyPartType = bodyPartType,
                    ColliderType = colliderType,
                    Absorbed = 0f,
                    Direction = damageInfo.Direction,
                    Point = damageInfo.HitPoint,
                    HitNormal = damageInfo.HitNormal,
                    PenetrationPower = damageInfo.PenetrationPower,
                    BlockedBy = damageInfo.BlockedBy,
                    DeflectedBy = damageInfo.DeflectedBy,
                    SourceId = damageInfo.SourceId,
                    ArmorDamage = damageInfo.ArmorDamage
                });

                return;
            }

            if (damageInfo.Player == null)
            {
                return;
            }

            if (!damageInfo.Player.iPlayer.IsYourPlayer)
            {
                return;
            }

            LastAggressor = damageInfo.Player.iPlayer;
            LastDamagedBodyPart = bodyPartType;
            LastBodyPart = bodyPartType;
            LastDamageInfo = damageInfo;
            LastDamageType = damageInfo.DamageType;
        }

        public override ShotInfoClass ApplyShot(DamageInfo damageInfo, EBodyPart bodyPartType, EBodyPartColliderType colliderType, EArmorPlateCollider armorPlateCollider, GStruct389 shotId)
        {
            ShotReactions(damageInfo, bodyPartType);

            if (damageInfo.DamageType == EDamageType.Sniper && FikaBackendUtils.IsServer)
            {
                PacketSender.DamagePackets.Enqueue(new()
                {
                    Damage = damageInfo.Damage,
                    DamageType = damageInfo.DamageType,
                    BodyPartType = bodyPartType,
                    ColliderType = colliderType,
                    ArmorPlateCollider = armorPlateCollider,
                    Absorbed = 0f,
                    Direction = damageInfo.Direction,
                    Point = damageInfo.HitPoint,
                    HitNormal = damageInfo.HitNormal,
                    PenetrationPower = damageInfo.PenetrationPower,
                    BlockedBy = damageInfo.BlockedBy,
                    DeflectedBy = damageInfo.DeflectedBy,
                    SourceId = damageInfo.SourceId,
                    ArmorDamage = damageInfo.ArmorDamage
                });

                return null;
            }

            if (damageInfo.Player != null)
            {
                LastAggressor = damageInfo.Player.iPlayer;
                LastDamagedBodyPart = bodyPartType;
                LastBodyPart = bodyPartType;
                LastDamageInfo = damageInfo;
                LastDamageType = damageInfo.DamageType;

                // There should never be other instances than CoopPlayer or its derived types
                CoopPlayer player = (CoopPlayer)damageInfo.Player.iPlayer;

                if (player.IsYourPlayer || player.IsAI)
                {
                    if (HealthController != null && !HealthController.IsAlive)
                    {
                        return null;
                    }

                    bool flag = !string.IsNullOrEmpty(damageInfo.DeflectedBy);
                    float damage = damageInfo.Damage;
                    List<ArmorComponent> list = ProceedDamageThroughArmor(ref damageInfo, colliderType, armorPlateCollider, true);
                    MaterialType materialType = flag ? MaterialType.HelmetRicochet : ((list == null || list.Count < 1) ? MaterialType.Body : list[0].Material);
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

                    PacketSender.DamagePackets.Enqueue(new()
                    {
                        Damage = damageInfo.Damage,
                        DamageType = damageInfo.DamageType,
                        BodyPartType = bodyPartType,
                        ColliderType = colliderType,
                        ArmorPlateCollider = armorPlateCollider,
                        Absorbed = 0f,
                        Direction = damageInfo.Direction,
                        Point = damageInfo.HitPoint,
                        HitNormal = damageInfo.HitNormal,
                        PenetrationPower = damageInfo.PenetrationPower,
                        BlockedBy = damageInfo.BlockedBy,
                        DeflectedBy = damageInfo.DeflectedBy,
                        SourceId = damageInfo.SourceId,
                        ArmorDamage = damageInfo.ArmorDamage,
                        ProfileId = damageInfo.Player.iPlayer.ProfileId,
                        Material = materialType
                    });

                    if (list != null)
                    {
                        QueueArmorDamagePackets([.. list]);
                    }

                    // Run this to get weapon skill
                    ManageAggressor(damageInfo, bodyPartType, colliderType);

                    return hitInfo;
                }
                return null;
            }

            return null;
        }

        public override void SetControllerInsteadRemovedOne(Item removingItem, Callback callback)
        {
            RemoveHandsControllerHandler handler = new(this, callback);
            _removeFromHandsCallback = callback;
            Proceed(false, new Callback<GInterface137>(handler.Handle), false);
        }

        public override void ApplyCorpseImpulse()
        {
            if (RagdollPacket.BodyPartColliderType != EBodyPartColliderType.None)
            {
                Collider collider = PlayerBones.BodyPartCollidersDictionary[RagdollPacket.BodyPartColliderType].Collider;
                Corpse.Ragdoll.ApplyImpulse(collider, RagdollPacket.Direction, RagdollPacket.Point, RagdollPacket.Force);
            }
        }

        public override void CreateMovementContext()
        {
            LayerMask movement_MASK = EFTHardSettings.Instance.MOVEMENT_MASK;
            MovementContext = ObservedMovementContext.Create(this, new Func<IAnimator>(GetBodyAnimatorCommon), new Func<ICharacterController>(GetCharacterControllerCommon), movement_MASK);
        }

        public override void OnHealthEffectAdded(IEffect effect)
        {
            // Remember to check if classes increment
            if (effect is GInterface260 && FractureSound != null && Singleton<BetterAudio>.Instantiated)
            {
                Singleton<BetterAudio>.Instance.PlayAtPoint(Position, FractureSound, CameraClass.Instance.Distance(Position),
                    BetterAudio.AudioSourceGroupType.Impacts, 15, 0.7f, EOcclusionTest.Fast, null, false);
            }
        }

        public override void OnHealthEffectRemoved(IEffect effect)
        {
            // Do nothing
        }

        #region proceed
        public override void Proceed(KnifeComponent knife, Callback<IKnifeController> callback, bool scheduled = true)
        {
            HandsControllerFactory factory = new(this, knifeComponent: knife);
            Func<KnifeController> func = new(factory.CreateObservedKnifeController);
            new Process<KnifeController, IKnifeController>(this, func, factory.knifeComponent.Item)
                .method_0(null, callback, scheduled);
        }

        public override void Proceed(GrenadeClass throwWeap, Callback<IHandsThrowController> callback, bool scheduled = true)
        {
            HandsControllerFactory factory = new(this, throwWeap);
            Func<GrenadeController> func = new(factory.CreateObservedGrenadeController);
            new Process<GrenadeController, IHandsThrowController>(this, func, throwWeap, false).method_0(null, callback, scheduled);
        }

        public override void Proceed(GrenadeClass throwWeap, Callback<GInterface145> callback, bool scheduled = true)
        {
            HandsControllerFactory factory = new(this, throwWeap);
            Func<QuickGrenadeThrowController> func = new(factory.CreateObservedQuickGrenadeController);
            new Process<QuickGrenadeThrowController, GInterface145>(this, func, throwWeap, false).method_0(null, callback, scheduled);
        }

        public override void Proceed(Weapon weapon, Callback<IFirearmHandsController> callback, bool scheduled = true)
        {
            HandsControllerFactory factory = new(this, weapon);
            Func<FirearmController> func = new(factory.CreateObservedFirearmController);
            new Process<FirearmController, IFirearmHandsController>(this, func, factory.item, true)
                .method_0(null, callback, scheduled);
        }

        public override void Proceed(MedsClass meds, EBodyPart bodyPart, Callback<GInterface142> callback, int animationVariant, bool scheduled = true)
        {
            HandsControllerFactory factory = new(this)
            {
                meds = meds,
                bodyPart = bodyPart,
                animationVariant = animationVariant
            };
            Func<MedsController> func = new(factory.CreateObservedMedsController);
            new Process<MedsController, GInterface142>(this, func, meds, false)
                .method_0(null, callback, scheduled);
        }

        public override void Proceed(FoodClass foodDrink, float amount, Callback<GInterface142> callback, int animationVariant, bool scheduled = true)
        {
            HandsControllerFactory factory = new(this)
            {
                food = foodDrink,
                amount = amount,
                animationVariant = animationVariant
            };
            Func<MedsController> func = new(factory.CreateObservedMedsController);
            new Process<MedsController, GInterface142>(this, func, foodDrink, false)
                .method_0(null, callback, scheduled);
        }
        #endregion

        public override void vmethod_3(EGesture gesture)
        {
            if (gesture == EGesture.Hello)
            {
                InteractionRaycast();
                if (InteractablePlayer != null)
                {
                    InteractablePlayer.ShowHelloNotification(Profile.Nickname);
                }
            }
            base.vmethod_3(gesture);
        }

        public override void OnFovUpdatedEvent(int fov)
        {
            // Do nothing
        }

        public override void ShowHelloNotification(string sender)
        {
            // Do nothing
        }

        public override void BtrInteraction()
        {
            // Do nothing
        }

        public override void DropCurrentController(Action callback, bool fastDrop, Item nextControllerItem = null)
        {
            base.DropCurrentController(callback, fastDrop, nextControllerItem);
        }

        public override void OnPhraseTold(EPhraseTrigger @event, TaggedClip clip, TagBank bank, PhraseSpeakerClass speaker)
        {
            method_34(clip);
        }

        public PlayerStatePacket Interpolate(in PlayerStatePacket newState, in PlayerStatePacket lastState)
        {
            float interpolate = 0.3f;

            method_58(newState.HasGround, newState.SurfaceSound);

            Rotation = new Vector2(Mathf.LerpAngle(MovementContext.Rotation.x, newState.Rotation.x, interpolate),
                Mathf.Lerp(MovementContext.Rotation.y, newState.Rotation.y, interpolate));

            HeadRotation = newState.HeadRotation;
            ProceduralWeaponAnimation.SetHeadRotation(HeadRotation);

            MovementContext.IsGrounded = newState.IsGrounded;

            EPlayerState oldMovementState = MovementContext.CurrentState.Name;
            EPlayerState newMovementState = newState.State;

            if (newMovementState == EPlayerState.Jump)
            {
                MovementContext.PlayerAnimatorEnableJump(true);
                if (isServer)
                {
                    MovementContext.method_2(1f); 
                }
            }
            if (MovementContext.PlayerAnimator.IsJumpSetted() && newState.IsGrounded)
            {
                MovementContext.PlayerAnimatorEnableJump(false);
                MovementContext.PlayerAnimatorEnableLanding(true);
            }
            if ((oldMovementState == EPlayerState.ProneIdle || oldMovementState == EPlayerState.ProneMove) && newMovementState != EPlayerState.ProneMove
                && newMovementState != EPlayerState.Transit2Prone && newMovementState != EPlayerState.ProneIdle)
            {
                MovementContext.IsInPronePose = false;
            }
            if ((newMovementState == EPlayerState.ProneIdle || newMovementState == EPlayerState.ProneMove) && oldMovementState != EPlayerState.ProneMove
                && oldMovementState != EPlayerState.Prone2Stand && oldMovementState != EPlayerState.Transit2Prone && oldMovementState != EPlayerState.ProneIdle)
            {
                MovementContext.IsInPronePose = true;
            }
            if (oldMovementState == EPlayerState.Sprint && newMovementState == EPlayerState.Transition)
            {
                MovementContext.UpdateSprintInertia();
                MovementContext.PlayerAnimatorEnableInert(false);
            }

            Physical.SerializationStruct = newState.Stamina;

            if (!Mathf.Approximately(MovementContext.Step, newState.Step))
            {
                CurrentManagedState.SetStep(newState.Step);
            }

            if (IsSprintEnabled != newState.IsSprinting)
            {
                CurrentManagedState.EnableSprint(newState.IsSprinting);
            }

            if (MovementContext.IsInPronePose != newState.IsProne)
            {
                MovementContext.IsInPronePose = newState.IsProne;
            }

            if (!Mathf.Approximately(PoseLevel, newState.PoseLevel))
            {
                MovementContext.SetPoseLevel(PoseLevel + (newState.PoseLevel - PoseLevel));
            }

            MovementContext.SetCurrentClientAnimatorStateIndex(newState.AnimatorStateIndex);
            MovementContext.SetCharacterMovementSpeed(newState.CharacterMovementSpeed, true);

            if (MovementContext.BlindFire != newState.Blindfire)
            {
                MovementContext.SetBlindFire(newState.Blindfire);
            }

            if (!IsInventoryOpened)
            {
                Move(Vector2.Lerp(newState.MovementDirection, lastState.MovementDirection, interpolate));
                if (isServer)
                {
                    MovementContext.method_1(newState.MovementDirection); 
                }
            }

            Vector3 newPosition = Vector3.Lerp(MovementContext.TransformPosition, newState.Position, interpolate);
            CharacterController.Move(newPosition - MovementContext.TransformPosition, interpolate);

            if (!Mathf.Approximately(MovementContext.Tilt, newState.Tilt))
            {
                MovementContext.SetTilt(newState.Tilt, true);
            }

            observedOverlap = newState.WeaponOverlap;
            leftStanceDisabled = newState.LeftStanceDisabled;
            MovementContext.SurfaceNormal = newState.SurfaceNormal;

            return newState;
        }

        public override void OnDead(EDamageType damageType)
        {
            StartCoroutine(DestroyNetworkedComponents());

            if (healthBar != null)
            {
                Destroy(healthBar);
            }

            if (FikaPlugin.ShowNotifications.Value)
            {
                if (!IsObservedAI)
                {
                    string nickname = !string.IsNullOrEmpty(Profile.Info.MainProfileNickname) ? Profile.Info.MainProfileNickname : Profile.Nickname;
                    if (damageType != EDamageType.Undefined)
                    {
                        NotificationManagerClass.DisplayWarningNotification($"Group member {ColorizeText(Colors.GREEN, nickname)} has died from {ColorizeText(Colors.RED, ("DamageType_" + damageType.ToString()).Localized())}");
                    }
                    else
                    {
                        NotificationManagerClass.DisplayWarningNotification($"Group member {ColorizeText(Colors.GREEN, nickname)} has died");
                    }
                }
                if (IsBoss(Profile.Info.Settings.Role, out string name) && IsObservedAI && LastAggressor != null)
                {
                    if (LastAggressor is CoopPlayer aggressor)
                    {
                        string aggressorNickname = !string.IsNullOrEmpty(LastAggressor.Profile.Info.MainProfileNickname) ? LastAggressor.Profile.Info.MainProfileNickname : LastAggressor.Profile.Nickname;
                        if (aggressor.gameObject.name.StartsWith("Player_") || aggressor.IsYourPlayer)
                        {
                            NotificationManagerClass.DisplayMessageNotification($"{ColorizeText(Colors.GREEN, LastAggressor.Profile.Info.MainProfileNickname)} killed boss {ColorizeText(Colors.BROWN, name)}", iconType: EFT.Communications.ENotificationIconType.Friend);
                        }
                    }
                }
            }

            /*if (Side == EPlayerSide.Savage)
            {
                if (LastAggressor != null)
                {
                    if (LastAggressor is CoopPlayer coopPlayer)
                    {
                        coopPlayer.hasKilledScav = true;
                    }
                }
            }

            if (hasKilledScav)
            {
                if (LastAggressor != null)
                {
                    if (LastAggressor.IsYourPlayer && LastAggressor.Side == EPlayerSide.Savage)
                    {
                        if (Side is EPlayerSide.Usec or EPlayerSide.Bear)
                        {
                            // This one is already handled by SPT, so we do not add directly to profile until they move it to client side
                            // They also do a flat value of 0.02 rather than 0.01 for 1 scav kill or 0.03 for >1
                            LastAggressor.Profile.EftStats.SessionCounters.AddDouble(0.02, [CounterTag.FenceStanding, EFenceStandingSource.ScavHelp]);
                            LastAggressor.Profile.FenceInfo.AddStanding(0.01, EFenceStandingSource.ScavHelp);
                        }
                        else if (Side == EPlayerSide.Savage)
                        {
                            LastAggressor.Profile.EftStats.SessionCounters.AddDouble(0.03, [CounterTag.FenceStanding, EFenceStandingSource.TraitorKill]);
                            LastAggressor.Profile.FenceInfo.AddStanding(0.03, EFenceStandingSource.TraitorKill);
                        }
                    }
                }
            }*/

            Singleton<BetterAudio>.Instance.ProtagonistHearingChanged -= SetSoundRollOff;
            /*if (FikaPlugin.CullPlayers.Value)
            {
                UnregisterCulling();
            }*/

            base.OnDead(damageType);
        }

        public override void OnBeenKilledByAggressor(IPlayer aggressor, DamageInfo damageInfo, EBodyPart bodyPart, EDamageType lethalDamageType)
        {
            // Only handle if it was ourselves as otherwise it's irrelevant
            if (LastAggressor.IsYourPlayer)
            {
                base.OnBeenKilledByAggressor(aggressor, damageInfo, bodyPart, lethalDamageType);
                return;
            }

            if (FikaPlugin.EasyKillConditions.Value)
            {
                if (aggressor.Profile.Info.GroupId == "Fika")
                {
                    CoopPlayer mainPlayer = (CoopPlayer)Singleton<GameWorld>.Instance.MainPlayer;
                    if (mainPlayer != null)
                    {
                        float distance = Vector3.Distance(aggressor.Position, Position);
                        mainPlayer.HandleTeammateKill(damageInfo, bodyPart, Side, Profile.Info.Settings.Role, ProfileId,
                            distance, CurrentHour, Inventory.EquippedInSlotsTemplateIds, HealthController.BodyPartEffects, TriggerZones,
                            (CoopPlayer)aggressor);
                    }
                }
            }
        }

        public override void SetupDogTag()
        {
            // Do nothing
        }

        public override void SetInventory(EquipmentClass equipmentClass)
        {
            Inventory.Equipment = equipmentClass;

            BindableState<Item> itemInHands = Traverse.Create(this).Field<BindableState<Item>>("_itemInHands").Value;
            if (HandsController != null && HandsController.Item != null)
            {
                Item item = FindItem(HandsController.Item.Id);
                if (item != null)
                {
                    itemInHands.Value = item;
                }
            }

            EquipmentSlot[] equipmentSlots = Traverse.Create<PlayerBody>().Field<EquipmentSlot[]>("SlotNames").Value;
            foreach (EquipmentSlot equipmentSlot in equipmentSlots)
            {
                Transform slotBone = PlayerBody.GetSlotBone(equipmentSlot);
                Transform alternativeHolsterBone = PlayerBody.GetAlternativeHolsterBone(equipmentSlot);
                PlayerBody.GClass1875 gclass = new(PlayerBody, Inventory.Equipment.GetSlot(equipmentSlot), slotBone, equipmentSlot, Inventory.Equipment.GetSlot(EquipmentSlot.Backpack), alternativeHolsterBone);
                PlayerBody.GClass1875 gclass2 = PlayerBody.SlotViews.AddOrReplace(equipmentSlot, gclass);
                if (gclass2 != null)
                {
                    gclass2.Dispose();
                }
            }

            //PlayerBody.Init(PlayerBody.BodyCustomization, Inventory.Equipment, shouldSet ? itemInHands : null, LayerMask.NameToLayer("Player"), Side);
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

        private IEnumerator DestroyNetworkedComponents()
        {
            yield return new WaitForSeconds(2);

            if (Speaker != null)
            {
                Speaker.Shut();
                Speaker.OnPhraseTold -= OnPhraseTold;
                Speaker.OnDestroy();
            }

            // Try to mitigate infinite firing loop further
            if (HandsController is CoopObservedFirearmController firearmController)
            {
                if (firearmController.WeaponSoundPlayer != null && firearmController.WeaponSoundPlayer.enabled)
                {
                    firearmController.WeaponSoundPlayer.enabled = false;
                }
            }
        }

        public void InitObservedPlayer(bool isDedicatedHost)
        {
            if (gameObject.name.StartsWith("Bot_"))
            {
                IsObservedAI = true;
            }

            PacketSender = gameObject.AddComponent<ObservedPacketSender>();
            Traverse playerTraverse = Traverse.Create(this);

            if (IsObservedAI)
            {
                GenericPacket genericPacket = new(EPackageType.LoadBot)
                {
                    NetId = NetId,
                    BotNetId = NetId
                };
                PacketSender.Writer.Reset();
                PacketSender.Client.SendData(PacketSender.Writer, ref genericPacket, LiteNetLib.DeliveryMethod.ReliableOrdered);

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

                /*if (FikaPlugin.CullPlayers.Value)
                {
                    SetupCulling();
                }*/
            }

            PacketReceiver = gameObject.AddComponent<PacketReceiver>();

            if (!IsObservedAI)
            {
                if (!isDedicatedHost)
                {
                    Profile.Info.GroupId = "Fika";
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

                if (FikaPlugin.ShowNotifications.Value && !isDedicatedHost)
                {
                    NotificationManagerClass.DisplayMessageNotification($"Group member {ColorizeText(Colors.GREEN, (Side == EPlayerSide.Savage ? Profile.Info.MainProfileNickname : Profile.Nickname))} has spawned",
                    EFT.Communications.ENotificationDurationType.Default, EFT.Communications.ENotificationIconType.Friend);
                }                

                Singleton<GameWorld>.Instance.MainPlayer.StatisticsManager.OnGroupMemberConnected(Inventory);

                RaycastCameraTransform = playerTraverse.Field<Transform>("_playerLookRaycastTransform").Value;
            }
        }

        private IEnumerator CreateHealthBar()
        {
            CoopGame coopGame = (CoopGame)Singleton<IFikaGame>.Instance;

            if (coopGame == null)
            {
                yield break;
            }

            while (coopGame.Status != GameStatus.Started)
            {
                yield return null;
            }

            healthBar = gameObject.AddComponent<FikaHealthBar>();

            yield break;
        }

        public override void LateUpdate()
        {
            DistanceDirty = true;
            OcclusionDirty = true;
            if (UpdateQueue == EUpdateQueue.FixedUpdate && !_manuallyUpdated)
            {
                return;
            }
            _manuallyUpdated = false;
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
            float fixedTime = Time.fixedTime;
            if (fixedTime - observedFixedTime > 1f)
            {
                observedFixedTime = fixedTime;
                OcclusionDirty = true;
                UpdateOcclusion();
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
                Transform transform = Singleton<PoolManager>.Instance.CreateFromPool<Transform>(new ResourceKey
                {
                    path = "assets/content/weapons/additional_hands/item_compass.bundle"
                });
                transform.SetParent(PlayerBones.Ribcage.Original, false);
                transform.localRotation = Quaternion.identity;
                transform.localPosition = Vector3.zero;
                method_29(transform.gameObject);
                Traverse.Create(this).Field("_compassInstantiated").SetValue(true);
                return;
            }
        }

        private void SetupCulling()
        {
            followerCullingObject = gameObject.AddComponent<FollowerCullingObject>();
            followerCullingObject.enabled = true;
            followerCullingObject.CullByDistanceOnly = false;
            followerCullingObject.Init(new Func<Transform>(GetPlayerBones));
            //followerCullingObject.SetParams(EFTHardSettings.Instance.CULLING_PLAYER_SPHERE_RADIUS, EFTHardSettings.Instance.CULLING_PLAYER_SPHERE_SHIFT, FikaPlugin.CullingRange.Value);
            //followerCullingObject.OnVisibilityChanged += OnObservedVisibilityChanged;

            if (_triggerColliderSearcher != null)
            {
                _triggerColliderSearcher.OnEnter += AddColliderReporters;
                _triggerColliderSearcher.OnExit += RemoveColliderReporters;
            }
        }

        private void UnregisterCulling()
        {
            if (followerCullingObject != null)
            {
                followerCullingObject.enabled = false;
            }

            if (_triggerColliderSearcher != null)
            {
                _triggerColliderSearcher.OnEnter -= AddColliderReporters;
                _triggerColliderSearcher.OnExit -= RemoveColliderReporters;
            }
        }

        private void AddColliderReporters(IPhysicsTrigger trigger)
        {
            ColliderReporter colliderReporter = trigger as ColliderReporter;
            if (colliderReporter != null)
            {
                for (int i = 0; i < colliderReporter.Owners.Count; i++)
                {
                    DisablerCullingObject disablerCullingObject = colliderReporter.Owners[i] as DisablerCullingObject;
                    if (disablerCullingObject != null)
                    {
                        cullingObjects.Add(disablerCullingObject);
                    }
                }
            }
        }

        private void RemoveColliderReporters(IPhysicsTrigger trigger)
        {
            ColliderReporter colliderReporter = trigger as ColliderReporter;
            if (colliderReporter != null)
            {
                for (int i = 0; i < colliderReporter.Owners.Count; i++)
                {
                    DisablerCullingObject disablerCullingObject = colliderReporter.Owners[i] as DisablerCullingObject;
                    if (disablerCullingObject != null)
                    {
                        cullingObjects.Remove(disablerCullingObject);
                    }
                }
            }
        }

        /*private void OnObservedVisibilityChanged(bool visible)
        {
            for (int i = 0; i < cullingRenderers.Count; i++)
            {
                cullingRenderers[i].forceRenderingOff = false;
            }
            cullingRenderers.Clear();
            if (HealthController.IsAlive)
            {
                IAnimator bodyAnimatorCommon = GetBodyAnimatorCommon();
                if (bodyAnimatorCommon.enabled != visible)
                {
                    bodyAnimatorCommon.enabled = visible;
                    if (HandsController.FirearmsAnimator != null && HandsController.FirearmsAnimator.Animator.enabled != visible)
                    {
                        HandsController.FirearmsAnimator.Animator.enabled = visible;
                    }
                }
                PlayerPoolObject playerPoolObject = gameObject.GetComponent<PlayerPoolObject>();
                if (playerPoolObject != null)
                {
                    foreach (Collider collider in playerPoolObject.Colliders)
                    {
                        if (collider.enabled != visible)
                        {
                            collider.enabled = visible;
                        }
                    }
                }
                PlayerBody.GetRenderersNonAlloc(cullingRenderers);
                for (int i = 0; i < cullingRenderers.Count; i++)
                {
                    cullingRenderers[i].forceRenderingOff = !visible;
                }
            }
        }*/

        private Transform GetPlayerBones()
        {
            return PlayerBones.BodyTransform.Original;
        }

        public override void OnVaulting()
        {
            // Do nothing
        }

        public override void ManualUpdate(float deltaTime, float? platformDeltaTime = null, int loop = 1)
        {
            _bodyupdated = true;
            _bodyTime = deltaTime;

            method_15(deltaTime);

            UpdateTriggerColliderSearcher(deltaTime, SqrCameraDistance < 1600);
        }

        public override void InitAudioController()
        {
            base.InitAudioController();
            Singleton<BetterAudio>.Instance.ProtagonistHearingChanged += SetSoundRollOff;
        }

        private void SetSoundRollOff()
        {
            if (NestedStepSoundSource != null)
            {
                NestedStepSoundSource.SetRolloff(60f * ProtagonistHearing);
            }
        }

        public override bool UpdateGrenadeAnimatorDuePoV()
        {
            return true;
        }

        public override void InitialProfileExamineAll()
        {
            // Do nothing
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
            if (healthBar != null)
            {
                Destroy(healthBar);
            }
            if (Singleton<BetterAudio>.Instantiated)
            {
                Singleton<BetterAudio>.Instance.ProtagonistHearingChanged -= SetSoundRollOff;
            }
            base.OnDestroy();
        }

        public override void Dispose()
        {
            /*if (FikaPlugin.CullPlayers.Value)
            {
                UnregisterCulling();
            }*/
            base.Dispose();
        }

        public override void SendHandsInteractionStateChanged(bool value, int animationId)
        {
            if (value)
            {
                MovementContext.SetBlindFire(0);
            }
        }

        public override void HandleDamagePacket(ref DamagePacket packet)
        {
            // Do nothing
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
                        CreateMedsController(packet.ItemId, packet.BodyPart, packet.Amount, packet.AnimationVariant);
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
                        if (HandsController == null || HandsController.Item.Id != packet.ItemId)
                        {
                            CreateFirearmController(packet.ItemId);
                        }
                        break;
                    }
                case EProceedType.Knife:
                    {
                        CreateKnifeController(packet.ItemId);
                        break;
                    }
            }
        }

        #region handControllers
        private void CreateHandsController(Func<AbstractHandsController> controllerFactory, Item item)
        {
            CreateHandsControllerHandler handler = new((item != null) ? method_71(item) : null);

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
                HandsController = null;
            }

            base.SpawnController(controllerFactory(), new Action(handler.DisposeHandler));
        }

        private void CreateEmptyHandsController()
        {
            CreateHandsController(new Func<AbstractHandsController>(ReturnEmptyHandsController), null);
        }

        private AbstractHandsController ReturnEmptyHandsController()
        {
            return CoopObservedEmptyHandsController.Create(this);
        }

        private void CreateFirearmController(string itemId)
        {
            CreateFirearmControllerHandler handler = new(this);

            if (MovementContext.StationaryWeapon != null && MovementContext.StationaryWeapon.Id == itemId)
            {
                handler.item = MovementContext.StationaryWeapon.Item;
            }
            else
            {
                handler.item = FindItem(itemId);
            }

            if (handler.item != null)
            {
                CreateHandsController(new Func<AbstractHandsController>(handler.ReturnController), handler.item);
            }
            else
            {
                FikaPlugin.Instance.FikaLogger.LogError("CreateFirearmController: item was null!");
            }
        }

        private void CreateGrenadeController(string itemId)
        {
            CreateGrenadeControllerHandler handler = new(this);

            Item item = FindItem(itemId);
            handler.item = item;
            if ((handler.item = item as GrenadeClass) != null)
            {
                CreateHandsController(new Func<AbstractHandsController>(handler.ReturnController), handler.item);
            }
            else
            {
                FikaPlugin.Instance.FikaLogger.LogError("CreateGrenadeController: item was null!");
            }
        }

        private void CreateMedsController(string itemId, EBodyPart bodyPart, float amount, int animationVariant)
        {
            CreateMedsControllerHandler handler = new(this, FindItem(itemId), bodyPart, amount, animationVariant);
            if (handler.item != null)
            {
                CreateHandsController(new Func<AbstractHandsController>(handler.ReturnController), handler.item);
            }
            else
            {
                FikaPlugin.Instance.FikaLogger.LogError("CreateMedsController: item was null!");
            }
        }

        private void CreateKnifeController(string itemId)
        {
            CreateKnifeControllerHandler handler = new(this);

            Item item = FindItem(itemId);
            handler.knife = item.GetItemComponent<KnifeComponent>();
            if (handler.knife != null)
            {
                CreateHandsController(new Func<AbstractHandsController>(handler.ReturnController), handler.knife.Item);
            }
            else
            {
                FikaPlugin.Instance.FikaLogger.LogError("CreateKnifeController: item was null!");
            }
        }

        private void CreateQuickGrenadeController(string itemId)
        {
            CreateQuickGrenadeControllerHandler handler = new(this);

            Item item = FindItem(itemId);
            if ((handler.item = item as GrenadeClass) != null)
            {
                CreateHandsController(new Func<AbstractHandsController>(handler.ReturnController), handler.item);
            }
            else
            {
                FikaPlugin.Instance.FikaLogger.LogError("CreateQuickGrenadeController: item was null!");
            }
        }

        private void CreateQuickKnifeController(string itemId)
        {
            CreateQuickKnifeControllerHandler handler = new(this);

            Item item = FindItem(itemId);
            handler.knife = item.GetItemComponent<KnifeComponent>();
            if (handler.knife != null)
            {
                CreateHandsController(new Func<AbstractHandsController>(handler.ReturnController), handler.knife.Item);
            }
            else
            {
                FikaPlugin.Instance.FikaLogger.LogError("CreateQuickKnifeController: item was null!");
            }
        }

        private void CreateQuickUseItemController(string itemId)
        {
            CreateQuickUseItemControllerHandler handler = new(this, FindItem(itemId));
            if (handler.item != null)
            {
                CreateHandsController(new Func<AbstractHandsController>(handler.ReturnController), handler.item);
            }
            else
            {
                FikaPlugin.Instance.FikaLogger.LogError("CreateMedsController: item was null!");
            }
        }

        public void SetAggressor(string killerId)
        {
            Player killer = Singleton<GameWorld>.Instance.GetEverExistedPlayerByID(killerId);
            if (killer != null)
            {
                LastAggressor = killer;
                /*if (killer.IsYourPlayer)
                {
                    Item weapon = FindItem(weaponId);
                    if (weapon != null)
                    {
                        LastDamageInfo = new()
                        {
                            Weapon = weapon
                        };
                    }
                }*/
            }
        }

        private class RemoveHandsControllerHandler(ObservedCoopPlayer coopPlayer, Callback callback)
        {
            private readonly ObservedCoopPlayer coopPlayer = coopPlayer;
            private readonly Callback callback = callback;

            public void Handle(Result<GInterface137> result)
            {
                if (coopPlayer._removeFromHandsCallback == callback)
                {
                    coopPlayer._removeFromHandsCallback = null;
                }
                callback.Invoke(result);
            }
        }

        private class CreateHandsControllerHandler(Class1082 setInHandsOperation)
        {
            public readonly Class1082 setInHandsOperation = setInHandsOperation;

            internal void DisposeHandler()
            {
                Class1082 handler = setInHandsOperation;
                if (handler == null)
                    return;
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
                return CoopObservedGrenadeController.Create(coopPlayer, (GrenadeClass)item);
            }
        }

        private class CreateMedsControllerHandler(ObservedCoopPlayer coopPlayer, Item item, EBodyPart bodyPart, float amount, int animationVariant)
        {
            private readonly ObservedCoopPlayer coopPlayer = coopPlayer;
            public readonly Item item = item;
            private readonly EBodyPart bodyPart = bodyPart;
            private readonly float amount = amount;
            private readonly int animationVariant = animationVariant;

            internal AbstractHandsController ReturnController()
            {
                return CoopObservedMedsController.Create(coopPlayer, item, bodyPart, amount, animationVariant);
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
                return CoopObservedQuickGrenadeController.Create(coopPlayer, (GrenadeClass)item);
            }
        }

        private class CreateQuickKnifeControllerHandler(ObservedCoopPlayer coopPlayer)
        {
            private readonly ObservedCoopPlayer coopPlayer = coopPlayer;
            public KnifeComponent knife;

            internal AbstractHandsController ReturnController()
            {
                return QuickKnifeKickController.smethod_8<QuickKnifeKickController>(coopPlayer, knife);
            }
        }

        private class CreateQuickUseItemControllerHandler(ObservedCoopPlayer coopPlayer, Item item)
        {
            private readonly ObservedCoopPlayer coopPlayer = coopPlayer;
            public readonly Item item = item;

            internal AbstractHandsController ReturnController()
            {
                return QuickUseItemController.smethod_5<QuickUseItemController>(coopPlayer, item);
            }
        }
    }
}

#endregion