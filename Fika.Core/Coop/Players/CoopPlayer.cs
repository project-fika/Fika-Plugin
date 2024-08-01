// © 2024 Lacyway All Rights Reserved

using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.Communications;
using EFT.GlobalEvents;
using EFT.HealthSystem;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.Vehicle;
using Fika.Core.Coop.ClientClasses;
using Fika.Core.Coop.Components;
using Fika.Core.Coop.Factories;
using Fika.Core.Coop.FreeCamera;
using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.ObservedClasses;
using Fika.Core.Coop.PacketHandlers;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using Fika.Core.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using static Fika.Core.Networking.FikaSerialization;
using static Fika.Core.Utils.ColorUtils;

namespace Fika.Core.Coop.Players
{
    /// <summary>
    /// <see cref="CoopPlayer"/> is the <see cref="LocalPlayer"/>, there can only be one <see cref="CoopPlayer"/> in every game and that is always yourself.
    /// </summary>
    public class CoopPlayer : LocalPlayer
    {
        #region Fields and Properties
        public PacketReceiver PacketReceiver;
        public IPacketSender PacketSender;
        private DateTime lastPingTime;
        public bool hasSkilledScav = false;
        //public bool hasKilledScav = false;
        public float observedOverlap = 0f;
        public bool leftStanceDisabled = false;
        public Vector2 LastDirection = Vector2.zero;
        public RagdollPacket RagdollPacket = default;
        public bool hasGround = false;
        public Transform RaycastCameraTransform;
        public int NetId;
        public bool IsObservedAI = false;
        public Dictionary<uint, Callback<EOperationStatus>> OperationCallbacks = [];
        public ClientMovementContext ClientMovementContext
        {
            get
            {
                return MovementContext as ClientMovementContext;
            }
        }
        #endregion

        public static async Task<LocalPlayer> Create(int playerId, Vector3 position, Quaternion rotation,
            string layerName, string prefix, EPointOfView pointOfView, Profile profile, bool aiControl,
            EUpdateQueue updateQueue, EUpdateMode armsUpdateMode, EUpdateMode bodyUpdateMode,
            CharacterControllerSpawner.Mode characterControllerMode, Func<float> getSensitivity,
            Func<float> getAimingSensitivity, IViewFilter filter, int netId, IStatisticsManager statisticsManager)
        {
            CoopPlayer player = Create<CoopPlayer>(ResourceKeyManagerAbstractClass.PLAYER_BUNDLE_NAME, playerId, position, updateQueue, armsUpdateMode,
                        bodyUpdateMode, characterControllerMode, getSensitivity, getAimingSensitivity, prefix, false);

            player.IsYourPlayer = true;
            player.NetId = netId;

            CoopClientInventoryController inventoryController = new(player, profile, false);

            ISession session = Singleton<ClientApplication<ISession>>.Instance.GetClientBackEndSession();

            LocalQuestControllerClass questController;
            if (FikaPlugin.Instance.SharedQuestProgression)
            {
                questController = new CoopClientSharedQuestController(profile, inventoryController, session, player);
            }
            else
            {
                questController = new LocalQuestControllerClass(profile, inventoryController, session, true);
            }
            questController.Init();
            questController.Run();

            GClass3233 achievementsController = new(profile, inventoryController, session, true);
            achievementsController.Init();
            achievementsController.Run();

            if (FikaBackendUtils.IsServer)
            {
                if (FikaBackendUtils.IsDedicated)
                {
                    player.PacketSender = player.gameObject.AddComponent<DedicatedPacketSender>();
                }
                else
                {
                    player.PacketSender = player.gameObject.AddComponent<ServerPacketSender>(); 
                }
            }
            else if (FikaBackendUtils.IsClient)
            {
                player.PacketSender = player.gameObject.AddComponent<ClientPacketSender>();
            }

            player.PacketReceiver = player.gameObject.AddComponent<PacketReceiver>();

            await player.Init(rotation, layerName, pointOfView, profile, inventoryController,
                new CoopClientHealthController(profile.Health, player, inventoryController, profile.Skills, aiControl),
                statisticsManager, questController, achievementsController, filter,
                EVoipState.NotAvailable, false, false);

            foreach (MagazineClass magazineClass in player.Inventory.GetPlayerItems(EPlayerItems.NonQuestItems).OfType<MagazineClass>())
            {
                player.InventoryControllerClass.StrictCheckMagazine(magazineClass, true, player.Profile.MagDrillsMastering, false, false);
            }

            player._handsController = EmptyHandsController.smethod_5<EmptyHandsController>(player);
            player._handsController.Spawn(1f, new Action(Class1526.class1526_0.method_0));

            player.AIData = new AIData(null, player);

            player.AggressorFound = false;

            player._animators[0].enabled = true;

            player.Profile.Info.MainProfileNickname = FikaBackendUtils.PMCName;

            return player;
        }

        public override void CreateMovementContext()
        {
            LayerMask movement_MASK = EFTHardSettings.Instance.MOVEMENT_MASK;
            if (FikaPlugin.Instance.UseInertia)
            {
                MovementContext = ClientMovementContext.Create(this, new Func<IAnimator>(GetBodyAnimatorCommon),
                    new Func<ICharacterController>(GetCharacterControllerCommon), movement_MASK);
            }
            else
            {
                MovementContext = NoInertiaMovementContext.Create(this, new Func<IAnimator>(GetBodyAnimatorCommon),
                    new Func<ICharacterController>(GetCharacterControllerCommon), movement_MASK);
            }
        }

        public override void OnSkillLevelChanged(GClass1778 skill)
        {
            NotificationManagerClass.DisplayNotification(new GClass2044(skill));
        }

        public override bool CheckSurface()
        {
            hasGround = base.CheckSurface();
            return hasGround;
        }

        public override void OnWeaponMastered(MasterSkillClass masterSkill)
        {
            NotificationManagerClass.DisplayMessageNotification(string.Format("MasteringLevelUpMessage".Localized(null),
                masterSkill.MasteringGroup.Id.Localized(null),
                masterSkill.Level.ToString()), ENotificationDurationType.Default, ENotificationIconType.Default, null);
        }

        public override void BtrInteraction(BTRSide btr, byte placeId, EInteractionType interaction)
        {
            base.BtrInteraction(btr, placeId, interaction);
            if (FikaBackendUtils.IsClient)
            {
                BTRInteractionPacket packet = new(NetId)
                {
                    HasInteractPacket = true,
                    InteractPacket = btr.GetInteractWithBtrPacket(placeId, interaction)
                };

                PacketSender.Writer.Reset();
                PacketSender.Client.SendData(PacketSender.Writer, ref packet, LiteNetLib.DeliveryMethod.ReliableOrdered);
            }
            else if (FikaBackendUtils.IsServer)
            {
                if (CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler))
                {
                    PlayerInteractPacket interactPacket = btr.GetInteractWithBtrPacket(placeId, interaction);

                    bool success = coopHandler.serverBTR.HostInteraction(this, interactPacket);
                    if (success)
                    {
                        BTRInteractionPacket packet = new(NetId)
                        {
                            HasInteractPacket = true,
                            InteractPacket = interactPacket
                        };

                        PacketSender.Writer.Reset();
                        PacketSender.Server.SendDataToAll(PacketSender.Writer, ref packet, LiteNetLib.DeliveryMethod.ReliableOrdered);
                    }
                }
            }
            UpdateInteractionCast();
        }

        public void ProcessInteractWithBTR(BTRInteractionPacket packet)
        {
            if (packet.HasInteractPacket)
            {
                if (packet.InteractPacket.HasInteraction)
                {
                    if (CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler))
                    {
                        if (coopHandler.clientBTR != null)
                        {
                            coopHandler.clientBTR.ClientInteraction(this, packet.InteractPacket);
                        }
                    }
                }
            }
            else if (IsYourPlayer)
            {
                GlobalEventHandlerClass.CreateEvent<BtrNotificationInteractionMessageEvent>().Invoke(PlayerId, EBtrInteractionStatus.Blacklisted);
            }
        }

        public override void ApplyDamageInfo(DamageInfo damageInfo, EBodyPart bodyPartType, EBodyPartColliderType colliderType, float absorbed)
        {
            if (IsYourPlayer)
            {
                if (damageInfo.Player != null)
                {
                    if (!FikaPlugin.Instance.FriendlyFire && damageInfo.Player.iPlayer is ObservedCoopPlayer observedCoopPlayer && !observedCoopPlayer.IsObservedAI)
                    {
                        return;
                    }
                }
                if (colliderType == EBodyPartColliderType.HeadCommon)
                {
                    damageInfo.Damage *= FikaPlugin.HeadDamageMultiplier.Value;
                }

                if (colliderType is EBodyPartColliderType.RightSideChestUp or EBodyPartColliderType.LeftSideChestUp)
                {
                    damageInfo.Damage *= FikaPlugin.ArmpitDamageMultiplier.Value;
                }

                if (bodyPartType is EBodyPart.Stomach)
                {
                    damageInfo.Damage *= FikaPlugin.StomachDamageMultiplier.Value;
                }
            }

            base.ApplyDamageInfo(damageInfo, bodyPartType, colliderType, absorbed);
        }

        public override ShotInfoClass ApplyShot(DamageInfo damageInfo, EBodyPart bodyPartType, EBodyPartColliderType colliderType, EArmorPlateCollider armorPlateCollider, GStruct389 shotId)
        {
            if (damageInfo.DamageType is EDamageType.Sniper or EDamageType.Landmine)
            {
                return SimulatedApplyShot(damageInfo, bodyPartType, colliderType, armorPlateCollider, shotId);
            }

            if (damageInfo.Player?.iPlayer is CoopBot)
            {
                return SimulatedApplyShot(damageInfo, bodyPartType, colliderType, armorPlateCollider, shotId);
            }

            return null;
        }

        private ShotInfoClass SimulatedApplyShot(DamageInfo damageInfo, EBodyPart bodyPartType, EBodyPartColliderType colliderType, EArmorPlateCollider armorPlateCollider, GStruct389 shotId)
        {
            ActiveHealthController activeHealthController = ActiveHealthController;
            if (activeHealthController != null && !activeHealthController.IsAlive)
            {
                return null;
            }
            bool flag = !string.IsNullOrEmpty(damageInfo.DeflectedBy);
            float damage = damageInfo.Damage;
            List<ArmorComponent> list = ProceedDamageThroughArmor(ref damageInfo, colliderType, armorPlateCollider, true);
            MaterialType materialType = flag ? MaterialType.HelmetRicochet : ((list == null || list.Count < 1) ? MaterialType.Body : list[0].Material);
            ShotInfoClass gclass = new()
            {
                PoV = PointOfView,
                Penetrated = string.IsNullOrEmpty(damageInfo.BlockedBy) || string.IsNullOrEmpty(damageInfo.DeflectedBy),
                Material = materialType
            };
            ApplyDamageInfo(damageInfo, bodyPartType, colliderType, 0f);
            ShotReactions(damageInfo, bodyPartType);
            float num = damage - damageInfo.Damage;
            if (num > 0)
            {
                damageInfo.DidArmorDamage = num;
            }
            ReceiveDamage(damageInfo.Damage, bodyPartType, damageInfo.DamageType, num, gclass.Material);

            if (list != null)
            {
                QueueArmorDamagePackets([.. list]);
            }

            return gclass;
        }

        #region Proceed
        public override void Proceed(bool withNetwork, Callback<GInterface137> callback, bool scheduled = true)
        {
            base.Proceed(withNetwork, callback, scheduled);
            PacketSender.CommonPlayerPackets.Enqueue(new()
            {
                HasProceedPacket = true,
                ProceedPacket = new()
                {
                    ProceedType = EProceedType.EmptyHands,
                    Scheduled = scheduled
                }
            });
        }

        public override void Proceed(FoodClass foodDrink, float amount, Callback<GInterface142> callback, int animationVariant, bool scheduled = true)
        {
            FoodControllerHandler handler = new(this, foodDrink, amount, EBodyPart.Head, animationVariant);

            Func<MedsController> func = new(handler.ReturnController);
            handler.process = new(this, func, foodDrink, false);
            handler.confirmCallback = new(handler.SendPacket);
            handler.process.method_0(new(handler.HandleResult), callback, scheduled);
        }

        public override void Proceed(Item item, Callback<IOnHandsUseCallback> callback, bool scheduled = true)
        {
            QuickUseItemControllerHandler handler = new(this, item);

            Func<QuickUseItemController> func = new(handler.ReturnController);
            handler.process = new(this, func, item, true);
            handler.confirmCallback = new(handler.SendPacket);
            handler.process.method_0(new(handler.HandleResult), callback, scheduled);
        }

        public override void Proceed(KnifeComponent knife, Callback<IKnifeController> callback, bool scheduled = true)
        {
            KnifeControllerHandler handler = new(this, knife);

            Func<KnifeController> func = new(handler.ReturnController);
            handler.process = new(this, func, handler.knife.Item, false);
            handler.confirmCallback = new(handler.SendPacket);
            handler.process.method_0(new(handler.HandleResult), callback, scheduled);
        }

        public override void Proceed(KnifeComponent knife, Callback<GInterface146> callback, bool scheduled = true)
        {
            QuickKnifeControllerHandler handler = new(this, knife);

            Func<QuickKnifeKickController> func = new(handler.ReturnController);
            handler.process = new(this, func, handler.knife.Item, true);
            handler.confirmCallback = new(handler.SendPacket);
            handler.process.method_0(new(handler.HandleResult), callback, scheduled);
        }

        public override void Proceed(MedsClass meds, EBodyPart bodyPart, Callback<GInterface142> callback, int animationVariant, bool scheduled = true)
        {
            MedsControllerHandler handler = new(this, meds, bodyPart, animationVariant);

            Func<MedsController> func = new(handler.ReturnController);
            handler.process = new(this, func, meds, false);
            handler.confirmCallback = new(handler.SendPacket);
            handler.process.method_0(new(handler.HandleResult), callback, scheduled);
        }

        public override void Proceed(GrenadeClass throwWeap, Callback<GInterface145> callback, bool scheduled = true)
        {
            QuickGrenadeControllerHandler handler = new(this, throwWeap);

            Func<QuickGrenadeThrowController> func = new(handler.ReturnController);
            handler.process = new(this, func, throwWeap, false);
            handler.confirmCallback = new(handler.SendPacket);
            handler.process.method_0(new(handler.HandleResult), callback, scheduled);
        }

        public override void Proceed(GrenadeClass throwWeap, Callback<IHandsThrowController> callback, bool scheduled = true)
        {
            GrenadeControllerHandler handler = new(this, throwWeap);

            Func<GrenadeController> func = new(handler.ReturnController);
            handler.process = new(this, func, throwWeap, false);
            handler.confirmCallback = new(handler.SendPacket);
            handler.process.method_0(new(handler.HandleResult), callback, scheduled);
        }

        public override void Proceed(Weapon weapon, Callback<IFirearmHandsController> callback, bool scheduled = true)
        {
            FirearmControllerHandler handler = new(this, weapon);
            bool flag = false;
            FirearmController firearmController;
            if ((firearmController = _handsController as FirearmController) != null)
            {
                flag = firearmController.CheckForFastWeaponSwitch(handler.weapon);
            }
            Func<FirearmController> func = new(handler.ReturnController);
            handler.process = new Process<FirearmController, IFirearmHandsController>(this, func, handler.weapon, flag);
            handler.confirmCallback = new(handler.SendPacket);
            handler.process.method_0(new(handler.HandleResult), callback, scheduled);
        }

        public override void Proceed<T>(Item item, Callback<GInterface141> callback, bool scheduled = true)
        {
            // what is this
            base.Proceed<T>(item, callback, scheduled);
        }
        #endregion

        public override void DropCurrentController(Action callback, bool fastDrop, Item nextControllerItem = null)
        {
            base.DropCurrentController(callback, fastDrop, nextControllerItem);

            /*PacketSender.CommonPlayerPackets.Enqueue(new()
            {
                HasDrop = true,
                DropPacket = new()
                {
                    FastDrop = fastDrop,
                    HasItemId = false
                }
            });*/
        }

        public override void OnBeenKilledByAggressor(IPlayer aggressor, DamageInfo damageInfo, EBodyPart bodyPart, EDamageType lethalDamageType)
        {
            base.OnBeenKilledByAggressor(aggressor, damageInfo, bodyPart, lethalDamageType);

            // Handle 'Help Scav' rep gains
            if (aggressor is CoopPlayer coopPlayer)
            {
                if (coopPlayer.Side == EPlayerSide.Savage)
                {
                    coopPlayer.Loyalty.method_1(this);
                }

                if (Side == EPlayerSide.Savage && coopPlayer.Side != EPlayerSide.Savage && !coopPlayer.hasSkilledScav)
                {
                    coopPlayer.hasSkilledScav = true;
                    return;
                }
                else if (Side != EPlayerSide.Savage && hasSkilledScav && aggressor.Side == EPlayerSide.Savage)
                {
                    coopPlayer.Profile?.FenceInfo?.AddStanding(Profile.Info.Settings.StandingForKill, EFT.Counters.EFenceStandingSource.ScavHelp);
                }
            }
        }

        public void HandleTeammateKill(DamageInfo damage, EBodyPart bodyPart,
            EPlayerSide playerSide, WildSpawnType role, string playerProfileId,
            float distance, int hour, List<string> targetEquipment,
            HealthEffects enemyEffects, List<string> zoneIds, CoopPlayer killer)
        {
            if (role != WildSpawnType.pmcBEAR)
            {
                if (role == WildSpawnType.pmcUSEC)
                {
                    playerSide = EPlayerSide.Usec;
                }
            }
            else
            {
                playerSide = EPlayerSide.Bear;
            }

            List<string> list = ["Any"];

            switch (playerSide)
            {
                case EPlayerSide.Usec:
                    list.Add("Usec");
                    list.Add("AnyPmc");
                    list.Add("Enemy");
                    break;
                case EPlayerSide.Bear:
                    list.Add("Bear");
                    list.Add("AnyPmc");
                    list.Add("Enemy");
                    break;
                case EPlayerSide.Savage:
                    list.Add("Savage");
                    list.Add("Bot");
                    break;
            }

            foreach (string value in list)
            {
                AbstractQuestControllerClass.CheckKillConditionCounter(value, playerProfileId, targetEquipment, damage.Weapon,
                                bodyPart, Location, distance, role.ToStringNoBox(), hour, enemyEffects,
                                killer.HealthController.BodyPartEffects, zoneIds, killer.HealthController.ActiveBuffsNames());

                AbstractAchievementControllerClass.CheckKillConditionCounter(value, playerProfileId, targetEquipment, damage.Weapon,
                    bodyPart, Location, distance, role.ToStringNoBox(), hour, enemyEffects,
                    killer.HealthController.BodyPartEffects, zoneIds, killer.HealthController.ActiveBuffsNames());
            }


        }

#if DEBUG
        public override void ShowStringNotification(string message)
        {
            if (IsYourPlayer)
            {
                ConsoleScreen.Log(message);
                FikaPlugin.Instance.FikaLogger.LogInfo(message);
            }
        }
#endif

        public override void SetInventoryOpened(bool opened)
        {
            if (this is ObservedCoopPlayer)
            {
                base.SetInventoryOpened(opened);
                return;
            }

            base.SetInventoryOpened(opened);
            PacketSender.CommonPlayerPackets.Enqueue(new()
            {
                HasInventoryChanged = true,
                SetInventoryOpen = opened
            });
        }

        public override void SetCompassState(bool value)
        {
            base.SetCompassState(value);
            PacketSender.FirearmPackets.Enqueue(new()
            {
                HasCompassChange = true,
                CompassState = value
            });
        }

        public void ClientApplyDamageInfo(DamageInfo damageInfo, EBodyPart bodyPartType, EBodyPartColliderType colliderType, float absorbed)
        {
            base.ApplyDamageInfo(damageInfo, bodyPartType, colliderType, absorbed);
        }

        public void ClientApplyShot(DamageInfo damageInfo, EBodyPart bodyPartType, EBodyPartColliderType colliderType, EArmorPlateCollider armorPlateCollider)
        {
            _ = base.ApplyShot(damageInfo, bodyPartType, colliderType, armorPlateCollider, GStruct389.EMPTY_SHOT_ID);
        }

        public override void SendHeadlightsPacket(bool isSilent)
        {
            FirearmLightStateStruct[] lightStates = _helmetLightControllers.Select(new Func<TacticalComboVisualController,
                FirearmLightStateStruct>(ClientPlayer.Class1456.class1456_0.method_0)).ToArray();

            if (PacketSender != null)
            {
                PacketSender.CommonPlayerPackets.Enqueue(new()
                {
                    HasHeadLightsPacket = true,
                    HeadLightsPacket = new()
                    {
                        Amount = lightStates.Count(),
                        IsSilent = isSilent,
                        LightStates = lightStates
                    }
                });
            }
        }

        public override void OnItemAddedOrRemoved(Item item, ItemAddress location, bool added)
        {
            base.OnItemAddedOrRemoved(item, location, added);
        }

        public override void OnPhraseTold(EPhraseTrigger @event, TaggedClip clip, TagBank bank, PhraseSpeakerClass speaker)
        {
            base.OnPhraseTold(@event, clip, bank, speaker);

            if (ActiveHealthController.IsAlive)
            {
                PacketSender.CommonPlayerPackets.Enqueue(new()
                {
                    Phrase = @event,
                    PhraseIndex = clip.NetId
                });
            }
        }

        public override void OperateStationaryWeapon(StationaryWeapon stationaryWeapon, GStruct170.EStationaryCommand command)
        {
            base.OperateStationaryWeapon(stationaryWeapon, command);
            PacketSender.CommonPlayerPackets.Enqueue(new()
            {
                HasStationaryPacket = true,
                StationaryPacket = new()
                {
                    Command = (StationaryPacket.EStationaryCommand)command,
                    Id = stationaryWeapon.Id
                }
            });
        }

        protected virtual void ReceiveSay(EPhraseTrigger trigger, int index)
        {
            if (HealthController.IsAlive)
            {
                Speaker.PlayDirect(trigger, index);
            }
        }

        // Start
        public override void vmethod_0(WorldInteractiveObject interactiveObject, InteractionResult interactionResult, Action callback)
        {
            if (this is ObservedCoopPlayer)
            {
                base.vmethod_0(interactiveObject, interactionResult, callback);
                return;
            }

            base.vmethod_0(interactiveObject, interactionResult, callback);

            CommonPlayerPacket packet = new()
            {
                HasWorldInteractionPacket = true,
                WorldInteractionPacket = new()
                {
                    InteractiveId = interactiveObject.Id,
                    InteractionType = interactionResult.InteractionType,
                    InteractionStage = EInteractionStage.Start,
                    ItemId = (interactionResult is KeyInteractionResultClass keyInteractionResult) ? keyInteractionResult.Key.Item.Id : string.Empty
                }
            };
            PacketSender.CommonPlayerPackets.Enqueue(packet);
        }

        // Execute
        public override void vmethod_1(WorldInteractiveObject door, InteractionResult interactionResult)
        {
            if (this is ObservedCoopPlayer)
            {
                base.vmethod_1(door, interactionResult);
                return;
            }

            base.vmethod_1(door, interactionResult);

            CommonPlayerPacket packet = new()
            {
                HasWorldInteractionPacket = true,
                WorldInteractionPacket = new()
                {
                    InteractiveId = door.Id,
                    InteractionType = interactionResult.InteractionType,
                    InteractionStage = EInteractionStage.Execute,
                    ItemId = (interactionResult is KeyInteractionResultClass keyInteractionResult) ? keyInteractionResult.Key.Item.Id : string.Empty
                }
            };
            PacketSender.CommonPlayerPackets.Enqueue(packet);

            UpdateInteractionCast();
        }

        public override void vmethod_3(EGesture gesture)
        {
            base.vmethod_3(gesture);
            PacketSender.FirearmPackets.Enqueue(new()
            {
                Gesture = gesture
            });
        }

        public override void ApplyCorpseImpulse()
        {
            Corpse.Ragdoll.ApplyImpulse(LastDamageInfo.HitCollider, LastDamageInfo.Direction, LastDamageInfo.HitPoint, _corpseAppliedForce);
        }

        public HealthSyncPacket SetupDeathPacket(GStruct346 packet)
        {
            float num = EFTHardSettings.Instance.HIT_FORCE;
            num *= 0.3f + 0.7f * Mathf.InverseLerp(50f, 20f, LastDamageInfo.PenetrationPower);
            _corpseAppliedForce = num;

            if (LastAggressor != null)
            {
                SetupDogTag();
            }

            return new(NetId)
            {
                Packet = packet,
                KillerId = !string.IsNullOrEmpty(KillerId) ? KillerId : null,
                RagdollPacket = new()
                {
                    BodyPartColliderType = LastDamageInfo.BodyPartColliderType,
                    Direction = LastDamageInfo.Direction,
                    Point = LastDamageInfo.HitPoint,
                    Force = _corpseAppliedForce,
                    OverallVelocity = Velocity
                },
                Equipment = Equipment,
                TriggerZones = TriggerZones.Count > 0 ? [.. TriggerZones] : null,
            };
        }

        public override void OnDead(EDamageType damageType)
        {
            base.OnDead(damageType);

            StartCoroutine(DestroyNetworkedComponents());
        }

        private IEnumerator DestroyNetworkedComponents()
        {
            yield return new WaitForSeconds(2);

            if (PacketSender != null)
            {
                PacketSender.DestroyThis();
            }
        }

        public override void Move(Vector2 direction)
        {
            if (direction.sqrMagnitude > 0)
            {
                base.Move(direction);
                LastDirection = direction;
            }
        }

        /// <summary>
        /// Used to determine whether this player was a boss
        /// </summary>
        /// <param name="wildSpawnType"></param>
        /// <returns>true if it's a boss, false if not</returns>
        public bool IsBoss(WildSpawnType wildSpawnType, out string name)
        {
            name = null;
            switch (wildSpawnType)
            {
                case WildSpawnType.bossBoar:
                    {
                        name = "Kaban";
                        break;
                    }
                case WildSpawnType.bossBully:
                    {
                        name = "Reshala";
                        break;
                    }
                case WildSpawnType.bossGluhar:
                    {
                        name = "Glukhar";
                        break;
                    }
                case WildSpawnType.bossKilla:
                    {
                        name = "Killa";
                        break;
                    }
                case WildSpawnType.bossKnight:
                    {
                        name = "Knight";
                        break;
                    }
                case WildSpawnType.bossKojaniy:
                    {
                        name = "Shturman";
                        break;
                    }
                case WildSpawnType.bossSanitar:
                    {
                        name = "Sanitar";
                        break;
                    }
                case WildSpawnType.bossTagilla:
                    {
                        name = "Tagilla";
                        break;
                    }
                case WildSpawnType.bossZryachiy:
                    {
                        name = "Zryachiy";
                        break;
                    }
                case WildSpawnType.followerBigPipe:
                    {
                        name = "Big Pipe";
                        break;
                    }
                case WildSpawnType.followerBirdEye:
                    {
                        name = "Bird Eye";
                        break;
                    }
                case WildSpawnType.sectantPriest:
                    {
                        name = "Cultist Priest";
                        break;
                    }
            }
            return name != null;
        }

        private void HandleInteractPacket(WorldInteractionPacket packet)
        {
            if (CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler))
            {
                WorldInteractiveObject worldInteractiveObject = Singleton<GameWorld>.Instance.FindDoor(packet.InteractiveId);
                if (worldInteractiveObject != null)
                {
                    if (worldInteractiveObject.isActiveAndEnabled)
                    {
                        InteractionResult interactionResult;
                        Action action;
                        if (packet.InteractionType == EInteractionType.Unlock)
                        {
                            KeyHandler keyHandler = new(this);

                            if (string.IsNullOrEmpty(packet.ItemId))
                            {
                                FikaPlugin.Instance.FikaLogger.LogWarning("HandleInteractPacket: ItemID was null!");
                                return;
                            }

                            Item keyItem = FindItem(packet.ItemId);
                            if (keyItem == null)
                            {
                                FikaPlugin.Instance.FikaLogger.LogWarning("HandleInteractPacket: Could not find item: " + packet.ItemId);
                                return;
                            }

                            KeyComponent keyComponent = keyItem.GetItemComponent<KeyComponent>();
                            if (keyComponent == null)
                            {
                                FikaPlugin.Instance.FikaLogger.LogWarning("HandleInteractPacket: keyComponent was null!");
                                return;
                            }

                            keyHandler.unlockResult = worldInteractiveObject.UnlockOperation(keyComponent, this);
                            if (keyHandler.unlockResult.Error != null)
                            {
                                FikaPlugin.Instance.FikaLogger.LogWarning("HandleInteractPacket: Error when processing unlockResult: " + keyHandler.unlockResult.Error);
                                return;
                            }

                            interactionResult = keyHandler.unlockResult.Value;
                            keyHandler.unlockResult.Value.RaiseEvents(_inventoryController, CommandStatus.Begin);
                            action = new(keyHandler.HandleKeyEvent);
                        }
                        else
                        {
                            interactionResult = new InteractionResult(packet.InteractionType);
                            action = null;
                        }

                        if (packet.InteractionStage == EInteractionStage.Start)
                        {
                            vmethod_0(worldInteractiveObject, interactionResult, action);
                            return;
                        }

                        if (packet.InteractionStage != EInteractionStage.Execute)
                        {
                            worldInteractiveObject.Interact(interactionResult);
                            return;
                        }

                        vmethod_1(worldInteractiveObject, interactionResult);
                    }

                }
                else
                {
                    FikaPlugin.Instance.FikaLogger.LogError("HandleInteractPacket: WorldInteractiveObject was null or disabled!");
                }
            }
            else
            {
                FikaPlugin.Instance.FikaLogger.LogError("HandleInteractPacket: CoopHandler was null!");
            }
        }

        public void Ping()
        {
            CoopGame coopGame = (CoopGame)Singleton<IFikaGame>.Instance;
            if (coopGame.Status != GameStatus.Started)
            {
                return;
            }

            if (lastPingTime < DateTime.Now.AddSeconds(-3))
            {
                Transform origin;
                FreeCameraController freeCamController = Singleton<FreeCameraController>.Instance;
                if (freeCamController != null && freeCamController.IsScriptActive)
                {
                    origin = freeCamController.CameraMain.gameObject.transform;
                }
                else
                {
                    origin = CameraPosition;
                }

                Ray sourceRaycast = new(origin.position + origin.forward / 2f,
                    origin.forward);
                int layer = LayerMask.GetMask(["HighPolyCollider", "Interactive", "Deadbody", "Player", "Loot", "Terrain"]);
                if (Physics.Raycast(sourceRaycast, out RaycastHit hit, 500f, layer))
                {
                    lastPingTime = DateTime.Now;
                    //GameObject gameObject = new("Ping", typeof(FikaPing));
                    //gameObject.transform.localPosition = hit.point;
                    Singleton<GUISounds>.Instance.PlayUISound(PingFactory.GetPingSound());
                    GameObject hitGameObject = hit.collider.gameObject;
                    int hitLayer = hitGameObject.layer;

                    PingFactory.EPingType pingType = PingFactory.EPingType.Point;
                    object userData = null;
                    string localeId = null;

#if DEBUG
                    ConsoleScreen.Log(statement: $"{hit.collider.GetFullPath()}: {LayerMask.LayerToName(hitLayer)}/{hitGameObject.name}"); 
#endif

                    if (LayerMask.LayerToName(hitLayer) == "Player")
                    {
                        if (hitGameObject.TryGetComponent(out Player player))
                        {
                            pingType = PingFactory.EPingType.Player;
                            userData = player;
                        }
                    }
                    else if (LayerMask.LayerToName(hitLayer) == "Deadbody")
                    {
                        pingType = PingFactory.EPingType.DeadBody;
                        userData = hitGameObject;
                    }
                    else if (hitGameObject.TryGetComponent(out LootableContainer container))
                    {
                        pingType = PingFactory.EPingType.LootContainer;
                        userData = container;
                        localeId = container.ItemOwner.Name;
                    }
                    else if (hitGameObject.TryGetComponent(out LootItem lootItem))
                    {
                        pingType = PingFactory.EPingType.LootItem;
                        userData = lootItem;
                        localeId = lootItem.Item.ShortName;
                    }
                    else if (hitGameObject.TryGetComponent(out Door door))
                    {
                        pingType = PingFactory.EPingType.Door;
                        userData = door;
                    }
                    else if (hitGameObject.TryGetComponent(out InteractableObject interactable))
                    {
                        pingType = PingFactory.EPingType.Interactable;
                        userData = interactable;
                    }

                    GameObject basePingPrefab = PingFactory.AbstractPing.pingBundle.LoadAsset<GameObject>("BasePingPrefab");
                    GameObject basePing = Instantiate(basePingPrefab);
                    Vector3 hitPoint = hit.point;
                    PingFactory.AbstractPing abstractPing = PingFactory.FromPingType(pingType, basePing);
                    Color pingColor = FikaPlugin.PingColor.Value;
                    pingColor = new(pingColor.r, pingColor.g, pingColor.b, 1);
                    // ref so that we can mutate it if we want to, ex: if I ping a switch I want it at the switch.gameObject.position + Vector3.up
                    abstractPing.Initialize(ref hitPoint, userData, pingColor);

                    GenericPacket genericPacket = new()
                    {
                        NetId = NetId,
                        PacketType = EPackageType.Ping,
                        PingLocation = hitPoint,
                        PingType = pingType,
                        PingColor = pingColor,
                        Nickname = Profile.Nickname,
                        LocaleId = string.IsNullOrEmpty(localeId) ? string.Empty : localeId
                    };

                    if (PacketSender != null)
                    {
                        PacketSender.SendPacket(ref genericPacket); 
                    }
                    else
                    {
                        NetManagerUtils.SendPacket(ref genericPacket);
                    }

                    if (FikaPlugin.PlayPingAnimation.Value)
                    {
                        vmethod_3(EGesture.ThatDirection);
                    }
                }
            }
        }

        public override void TryInteractionCallback(LootableContainer container)
        {
            LootableContainerInteractionHandler handler = new(this, container);
            if (handler.container != null && _openAction != null)
            {
                _openAction(new Action(handler.Handle));
            }
            _openAction = null;
        }

        public void SetupMainPlayer()
        {
            // Set own group id, ignore if dedicated
            if (!Profile.Info.Nickname.Contains("dedicated_"))
            {
                Profile.Info.GroupId = "Fika";
            }

            // Setup own dog tag
            if (Side != EPlayerSide.Savage)
            {
                FikaPlugin.Instance.FikaLogger.LogInfo("Setting up DogTag");
                if (Equipment.GetSlot(EquipmentSlot.Dogtag).ContainedItem != null)
                {
                    GStruct414<GClass2801> result = InteractionsHandlerClass.Remove(Equipment.GetSlot(EquipmentSlot.Dogtag).ContainedItem, _inventoryController, false, true);
                    if (result.Error != null)
                    {
                        FikaPlugin.Instance.FikaLogger.LogWarning("CoopPlayer::SetupMainPlayer: Error removing dog tag!");
                    }
                }

                string templateId = GetDogTagTemplateId();

                if (!string.IsNullOrEmpty(templateId))
                {
                    Item item = Singleton<ItemFactory>.Instance.CreateItem(MongoID.Generate(), templateId, null);

                    Slot dogtagSlot = Equipment.GetSlot(EquipmentSlot.Dogtag);
                    ItemFilter[] filters = dogtagSlot.Filters; // We need to temporarily remove and then re-add these as BSG did not include the new dog tags in their ItemFilter[]
                    dogtagSlot.Filters = null;
                    GStruct416<int> addResult = dogtagSlot.Add(item, false);
                    dogtagSlot.Filters = filters;

                    if (addResult.Error != null)
                    {
                        FikaPlugin.Instance.FikaLogger.LogError("CoopPlayer::SetupMainPlayer: Error adding dog tag to slot: " + addResult.Error);
                    }

                    DogtagComponent dogtagComponent = item.GetItemComponent<DogtagComponent>();
                    if (dogtagComponent != null)
                    {
                        dogtagComponent.ProfileId = ProfileId;
                        dogtagComponent.GroupId = Profile.Info.GroupId;
                    }
                    else
                    {
                        FikaPlugin.Instance.FikaLogger.LogWarning("Unable to find DogTagComponent");
                    }
                }
                else
                {
                    FikaPlugin.Instance.FikaLogger.LogError("Could not get templateId for DogTag!");
                }
            }

            if (!string.IsNullOrEmpty(Location))
            {
                // Delete labs card on labs
                if (Location.ToLower() == "laboratory")
                {
                    foreach (Item item in Inventory.AllRealPlayerItems)
                    {
                        if (item.TemplateId == "5c94bbff86f7747ee735c08f")
                        {
                            InteractionsHandlerClass.Remove(item, _inventoryController, false, true);
                            break;
                        }
                    }
                }
            }
            else
            {
                FikaPlugin.Instance.FikaLogger.LogError("CoopPlayer::SetupMainPlayer: Location was null!");
            }
        }

        private string GetDogTagTemplateId()
        {
            if (Side is EPlayerSide.Usec)
            {
                switch (Profile.Info.SelectedMemberCategory)
                {
                    case EMemberCategory.Default:
                        return "59f32c3b86f77472a31742f0";
                    case EMemberCategory.UniqueId:
                        return "6662e9f37fa79a6d83730fa0";
                    case EMemberCategory.Unheard:
                        return "6662ea05f6259762c56f3189";
                }
            }
            else if (Side is EPlayerSide.Bear)
            {
                switch (Profile.Info.SelectedMemberCategory)
                {
                    case EMemberCategory.Default:
                        return "59f32bb586f774757e1e8442";
                    case EMemberCategory.UniqueId:
                        return "6662e9aca7e0b43baa3d5f74";
                    case EMemberCategory.Unheard:
                        return "6662e9cda7e0b43baa3d5f76";
                }
            }

            return string.Empty;
        }

        public virtual void HandleCommonPacket(in CommonPlayerPacket packet)
        {
            if (packet.Phrase != EPhraseTrigger.PhraseNone)
            {
                ReceiveSay(packet.Phrase, packet.PhraseIndex);
            }

            if (packet.HasWorldInteractionPacket)
            {
                HandleInteractPacket(packet.WorldInteractionPacket);
            }

            if (packet.HasContainerInteractionPacket)
            {
                WorldInteractiveObject lootableContainer = Singleton<GameWorld>.Instance.FindDoor(packet.ContainerInteractionPacket.InteractiveId);
                if (lootableContainer != null)
                {
                    if (lootableContainer.isActiveAndEnabled)
                    {
                        string methodName = string.Empty;
                        switch (packet.ContainerInteractionPacket.InteractionType)
                        {
                            case EInteractionType.Open:
                                methodName = "Open";
                                break;
                            case EInteractionType.Close:
                                methodName = "Close";
                                break;
                            case EInteractionType.Unlock:
                                methodName = "Unlock";
                                break;
                            case EInteractionType.Breach:
                                break;
                            case EInteractionType.Lock:
                                methodName = "Lock";
                                break;
                        }

                        if (!string.IsNullOrEmpty(methodName))
                        {
                            void Interact() => lootableContainer.Invoke(methodName, 0);

                            if (packet.ContainerInteractionPacket.InteractionType == EInteractionType.Unlock)
                            {
                                Interact();
                            }
                            else
                            {
                                lootableContainer.StartBehaviourTimer(EFTHardSettings.Instance.DelayToOpenContainer, Interact);
                            }
                        }
                    }
                }
                else
                {
                    FikaPlugin.Instance.FikaLogger.LogError("CommonPlayerPacket::ContainerInteractionPacket: LootableContainer was null!");
                }
            }

            if (packet.HasProceedPacket)
            {
                if (this is ObservedCoopPlayer observedCoopPlayer)
                {
                    observedCoopPlayer.HandleProceedPacket(packet.ProceedPacket);
                }
            }

            if (packet.HasHeadLightsPacket)
            {
                try
                {
                    if (_helmetLightControllers != null)
                    {
                        for (int i = 0; i < _helmetLightControllers.Count(); i++)
                        {
                            _helmetLightControllers.ElementAt(i)?.LightMod?.SetLightState(packet.HeadLightsPacket.LightStates[i]);
                        }
                        if (!packet.HeadLightsPacket.IsSilent)
                        {
                            SwitchHeadLightsAnimation();
                        }
                    }
                }
                catch (Exception)
                {
                    // Do nothing
                }
            }

            if (packet.HasInventoryChanged)
            {
                base.SetInventoryOpened(packet.SetInventoryOpen);
            }

            if (packet.HasDrop)
            {
                if (packet.DropPacket.HasItemId)
                {
                    Item item = FindItem(packet.ProceedPacket.ItemId);
                    base.DropCurrentController(null, packet.DropPacket.FastDrop, item ?? null);
                }
                else
                {
                    base.DropCurrentController(null, packet.DropPacket.FastDrop, null);
                }
            }

            if (packet.HasStationaryPacket)
            {
                StationaryWeapon stationaryWeapon = (packet.StationaryPacket.Command == StationaryPacket.EStationaryCommand.Occupy) ? Singleton<GameWorld>.Instance.FindStationaryWeapon(packet.StationaryPacket.Id) : null;
                base.OperateStationaryWeapon(stationaryWeapon, (GStruct170.EStationaryCommand)packet.StationaryPacket.Command);
            }

            if (packet.Pickup)
            {
                MovementContext.SetInteractInHands(packet.Pickup, packet.PickupAnimation);
            }

            if (packet.HasVaultPacket)
            {
                DoObservedVault(packet.VaultPacket);
            }
        }

        public virtual void DoObservedVault(VaultPacket vaultPacket)
        {

        }

        public void HandleCallbackFromServer(in OperationCallbackPacket operationCallbackPacket)
        {
            if (OperationCallbacks.TryGetValue(operationCallbackPacket.CallbackId, out Callback<EOperationStatus> callback))
            {
                if (operationCallbackPacket.OperationStatus != EOperationStatus.Started)
                {
                    OperationCallbacks.Remove(operationCallbackPacket.CallbackId);
                }
                if (operationCallbackPacket.OperationStatus != EOperationStatus.Failed)
                {
                    callback(new Result<EOperationStatus>(operationCallbackPacket.OperationStatus));
                }
                else
                {
                    callback(new Result<EOperationStatus>(EOperationStatus.Failed)
                    {
                        Error = operationCallbackPacket.Error
                    });
                }
            }
            else
            {
                FikaPlugin.Instance.FikaLogger.LogError($"Could not find CallbackId {operationCallbackPacket.CallbackId}!");
            }
        }

        public virtual void HandleInventoryPacket(in InventoryPacket packet)
        {
            if (packet.HasItemControllerExecutePacket)
            {
                if (_inventoryController != null)
                {
                    using MemoryStream memoryStream = new(packet.ItemControllerExecutePacket.OperationBytes);
                    using BinaryReader binaryReader = new(memoryStream);
                    try
                    {
                        GStruct411 result = ToInventoryOperation(binaryReader.ReadPolymorph<GClass1543>());

                        InventoryOperationHandler opHandler = new(result);

                        opHandler.opResult.Value.vmethod_0(new Callback(opHandler.HandleResult), false);

                        // TODO: Hacky workaround to fix errors due to each client generating new IDs. Might need to find a more 'elegant' solution later.
                        // Unknown what problems this might cause so far.
                        if (result.Value is UnloadOperationClass unloadOperation)
                        {
                            if (unloadOperation.InternalOperation is SplitOperationClass internalSplitOperation)
                            {
                                Item item = internalSplitOperation.To.Item;
                                if (item != null)
                                {
                                    if (item.Id != internalSplitOperation.CloneId && item.TemplateId == internalSplitOperation.Item.TemplateId)
                                    {
                                        item.Id = internalSplitOperation.CloneId;
                                    }
                                    else
                                    {
                                        FikaPlugin.Instance.FikaLogger.LogWarning($"Matching failed: ItemID: {item.Id}, SplitOperationItemID: {internalSplitOperation.To.Item.Id}");
                                    }
                                }
                                else
                                {
                                    FikaPlugin.Instance.FikaLogger.LogError("Split: Item was null");
                                }
                            }
                        }

                        // TODO: Same as above.
                        if (result.Value is SplitOperationClass splitOperation)
                        {
                            Item item = splitOperation.To.Item;
                            if (item != null)
                            {
                                if (item.Id != splitOperation.CloneId && item.TemplateId == splitOperation.Item.TemplateId)
                                {
                                    item.Id = splitOperation.CloneId;
                                }
                                else
                                {
                                    FikaPlugin.Instance.FikaLogger.LogWarning($"Matching failed: ItemID: {item.Id}, SplitOperationItemID: {splitOperation.To.Item.Id}");
                                }
                            }
                            else
                            {
                                FikaPlugin.Instance.FikaLogger.LogError("Split: Item was null");
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        FikaPlugin.Instance.FikaLogger.LogError($"ItemControllerExecutePacket::Exception thrown: {exception}");
                        if (FikaBackendUtils.IsServer)
                        {
                            OperationCallbackPacket callbackPacket = new(NetId, packet.ItemControllerExecutePacket.CallbackId, EOperationStatus.Failed);
                            Singleton<FikaServer>.Instance.SendDataToAll(new(), ref callbackPacket, LiteNetLib.DeliveryMethod.ReliableOrdered);
                        }
                    }
                }
                else
                {
                    FikaPlugin.Instance.FikaLogger.LogError("ItemControllerExecutePacket: inventory was null!");
                    if (FikaBackendUtils.IsServer)
                    {
                        OperationCallbackPacket callbackPacket = new(NetId, packet.ItemControllerExecutePacket.CallbackId, EOperationStatus.Failed);
                        Singleton<FikaServer>.Instance.SendDataToAll(new(), ref callbackPacket, LiteNetLib.DeliveryMethod.ReliableOrdered);
                    }
                }
            }

            // Currently unused
            /*if (packet.HasSearchPacket)
            {
                if (!packet.SearchPacket.IsStop)
                {
                    if (FindItem(packet.SearchPacket.ItemId) is SearchableItemClass item)
                    {
                        GClass2869 operation = new((ushort)packet.SearchPacket.OperationId, _inventoryController, item);
                        _inventoryController.Execute(operation, null);
                    }
                }
                else if (packet.SearchPacket.IsStop)
                {
                    if (FindItem(packet.SearchPacket.ItemId) is SearchableItemClass item)
                    {
                        GClass2869 operation = new((ushort)packet.SearchPacket.OperationId, _inventoryController, item);
                        _inventoryController.ExecuteStop(operation);
                    }
                }
            }*/
        }

        public virtual void HandleWeaponPacket(in WeaponPacket packet)
        {
            if (HandsController is CoopObservedFirearmController firearmController)
            {
                firearmController.HandleFirearmPacket(packet, _inventoryController);
            }

            if (packet.Gesture != EGesture.None)
            {
                vmethod_3(packet.Gesture);
            }

            if (packet.Loot)
            {
                HandsController.Loot(packet.Loot);
            }

            if (packet.HasGrenadePacket)
            {
                if (HandsController is CoopObservedGrenadeController grenadeController)
                {
                    switch (packet.GrenadePacket.PacketType)
                    {
                        case GrenadePacket.GrenadePacketType.ExamineWeapon:
                            {
                                grenadeController.ExamineWeapon();
                                break;
                            }
                        case GrenadePacket.GrenadePacketType.HighThrow:
                            {
                                grenadeController.HighThrow();
                                break;
                            }
                        case GrenadePacket.GrenadePacketType.LowThrow:
                            {
                                grenadeController.LowThrow();
                                break;
                            }
                        case GrenadePacket.GrenadePacketType.PullRingForHighThrow:
                            {
                                grenadeController.PullRingForHighThrow();
                                break;
                            }
                        case GrenadePacket.GrenadePacketType.PullRingForLowThrow:
                            {
                                grenadeController.PullRingForLowThrow();
                                break;
                            }
                    }
                    if (packet.GrenadePacket.HasGrenade)
                    {
                        grenadeController.SpawnGrenade(0f, packet.GrenadePacket.GrenadePosition, packet.GrenadePacket.GrenadeRotation, packet.GrenadePacket.ThrowForce, packet.GrenadePacket.LowThrow);
                    }
                }
                else if (HandsController is CoopObservedQuickGrenadeController quickGrenadeController)
                {
                    if (packet.GrenadePacket.HasGrenade)
                    {
                        quickGrenadeController.SpawnGrenade(0f, packet.GrenadePacket.GrenadePosition, packet.GrenadePacket.GrenadeRotation, packet.GrenadePacket.ThrowForce, packet.GrenadePacket.LowThrow);
                    }
                }
                else
                {
                    FikaPlugin.Instance.FikaLogger.LogError($"HandleFirearmPacket::GrenadePacket: HandsController was not of type CoopObservedGrenadeController! Was {HandsController.GetType().Name}");
                }
            }

            if (packet.CancelGrenade)
            {
                if (HandsController is CoopObservedGrenadeController grenadeController)
                {
                    grenadeController.vmethod_3();
                }
            }

            if (packet.HasCompassChange)
            {
                if (HandsController is ItemHandsController handsController)
                {
                    handsController.ApplyCompassPacket(new()
                    {
                        Toggle = true,
                        Status = packet.CompassState
                    });
                }
            }

            if (packet.HasKnifePacket)
            {
                if (HandsController is CoopObservedKnifeController knifeController)
                {
                    if (packet.KnifePacket.Examine)
                    {
                        knifeController.ExamineWeapon();
                    }

                    if (packet.KnifePacket.Kick)
                    {
                        knifeController.MakeKnifeKick();
                    }

                    if (packet.KnifePacket.AltKick)
                    {
                        knifeController.MakeAlternativeKick();
                    }

                    if (packet.KnifePacket.BreakCombo)
                    {
                        knifeController.BrakeCombo();
                    }
                }
                else
                {
                    FikaPlugin.Instance.FikaLogger.LogError($"HandleFirearmPacket::KnifePacket: HandsController was not of type CoopObservedKnifeController! Was {HandsController.GetType().Name}");
                }
            }
        }

        public void QueueArmorDamagePackets(ArmorComponent[] armorComponents)
        {
            int amount = armorComponents.Length;
            if (amount > 0)
            {
                string[] ids = new string[amount];
                float[] durabilities = new float[amount];

                for (int i = 0; i < amount; i++)
                {
                    ids[i] = armorComponents[i].Item.Id;
                    durabilities[i] = armorComponents[i].Repairable.Durability;
                }

                PacketSender.ArmorDamagePackets.Enqueue(new()
                {
                    ItemIds = ids,
                    Durabilities = durabilities,
                });
            }
        }

        public virtual void HandleDamagePacket(ref DamagePacket packet)
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
                SourceId = packet.SourceId,
                ArmorDamage = packet.ArmorDamage
            };

            if (!string.IsNullOrEmpty(packet.ProfileId))
            {
                IPlayerOwner player = Singleton<GameWorld>.Instance.GetAlivePlayerBridgeByProfileID(packet.ProfileId);

                if (player != null)
                {
                    damageInfo.Player = player;
                    if (IsYourPlayer)
                    {
                        if (!FikaPlugin.Instance.FriendlyFire && damageInfo.Player.iPlayer is ObservedCoopPlayer observedCoopPlayer && !observedCoopPlayer.IsObservedAI)
                        {
                            return;
                        }
                    }
                }

                // TODO: Fix this and consistently get the correct data...
                if (Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(packet.ProfileId).HandsController.Item is Weapon weapon)
                {
                    damageInfo.Weapon = weapon;
                }
            }

            ShotReactions(damageInfo, packet.BodyPartType);
            ReceiveDamage(damageInfo.Damage, packet.BodyPartType, damageInfo.DamageType, packet.Absorbed, packet.Material);
            ClientApplyDamageInfo(damageInfo, packet.BodyPartType, packet.ColliderType, packet.Absorbed);
            //ClientApplyShot(damageInfo, packet.BodyPartType, packet.ColliderType, packet.ArmorPlateCollider);
        }

        public void HandleArmorDamagePacket(ref ArmorDamagePacket packet)
        {
            for (int i = 0; i < packet.ItemIds.Length; i++)
            {
                _preAllocatedArmorComponents.Clear();
                Inventory.GetPutOnArmorsNonAlloc(_preAllocatedArmorComponents);
                foreach (ArmorComponent armorComponent in _preAllocatedArmorComponents)
                {
                    if (armorComponent.Item.Id == packet.ItemIds[i])
                    {
                        armorComponent.Repairable.Durability = packet.Durabilities[i];
                        armorComponent.Buff.TryDisableComponent(armorComponent.Repairable.Durability);
                        armorComponent.Item.RaiseRefreshEvent(false, false);
                        return;
                    }
                }
                GStruct416<Item> gstruct = Singleton<GameWorld>.Instance.FindItemById(packet.ItemIds[i]);
                if (gstruct.Failed)
                {
                    FikaPlugin.Instance.FikaLogger.LogError("HandleArmorDamagePacket: " + gstruct.Error);
                    return;
                }
                ArmorComponent itemComponent = gstruct.Value.GetItemComponent<ArmorComponent>();
                if (itemComponent != null)
                {
                    itemComponent.Repairable.Durability = packet.Durabilities[i];
                    itemComponent.Buff.TryDisableComponent(itemComponent.Repairable.Durability);
                    itemComponent.Item.RaiseRefreshEvent(false, false);
                }
            }
        }

        public virtual void SetupDogTag()
        {
            if (LastAggressor != null)
            {
                Item item = Equipment.GetSlot(EquipmentSlot.Dogtag).ContainedItem;
                if (item != null)
                {
                    DogtagComponent dogtagComponent = item.GetItemComponent<DogtagComponent>();
                    if (dogtagComponent != null)
                    {
                        dogtagComponent.Item.SpawnedInSession = true;
                        dogtagComponent.AccountId = AccountId;
                        dogtagComponent.ProfileId = ProfileId;
                        dogtagComponent.Nickname = Profile.Nickname;
                        dogtagComponent.KillerAccountId = KillerAccountId;
                        dogtagComponent.KillerProfileId = KillerId;
                        dogtagComponent.KillerName = LastAggressor.Profile.Nickname;
                        dogtagComponent.Side = Side;
                        dogtagComponent.Level = Profile.Info.Experience > 0 ? Profile.Info.Level : 1;
                        dogtagComponent.Time = DateTime.Now;
                        dogtagComponent.Status = "Killed by ";
                        dogtagComponent.WeaponName = LastDamageInfo.Weapon != null ? LastDamageInfo.Weapon.Name : "Unknown";
                        dogtagComponent.GroupId = GroupId;
                    }
                }
            }
        }

        public void CheckAndResetControllers(ExitStatus exitStatus, float pastTime, string locationId, string exitName)
        {
            _questController?.CheckExitConditionCounters(exitStatus, pastTime, locationId, exitName, HealthController.BodyPartEffects, TriggerZones);
            _questController?.ResetCurrentNullableCounters();

            _achievementsController?.CheckExitConditionCounters(exitStatus, pastTime, locationId, exitName, HealthController.BodyPartEffects, TriggerZones);
            _achievementsController?.ResetCurrentNullableCounters();
        }

        public virtual void SetInventory(EquipmentClass equipmentClass)
        {
            // Do nothing
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
        }

        public override void Dispose()
        {
            base.Dispose();
            if (PacketSender != null)
            {
                PacketSender.DestroyThis();
            }
        }

        public override void SendHandsInteractionStateChanged(bool value, int animationId)
        {
            base.SendHandsInteractionStateChanged(value, animationId);
            if (value)
            {
                PacketSender.CommonPlayerPackets.Enqueue(new()
                {
                    Pickup = value,
                    PickupAnimation = animationId
                });
            }
        }

        public override void OnVaulting()
        {
            PacketSender.CommonPlayerPackets.Enqueue(new()
            {
                HasVaultPacket = true,
                VaultPacket = new()
                {
                    VaultingStrategy = VaultingParameters.GetVaultingStrategy(),
                    VaultingPoint = VaultingParameters.MaxWeightPointPosition,
                    VaultingHeight = VaultingParameters.VaultingHeight,
                    VaultingLength = VaultingParameters.VaultingLength,
                    VaultingSpeed = MovementContext.VaultingSpeed,
                    BehindObstacleHeight = VaultingParameters.BehindObstacleRatio,
                    AbsoluteForwardVelocity = VaultingParameters.AbsoluteForwardVelocity
                }
            });
        }

        public Item FindItem(string itemId, bool questItem = false)
        {
            if (questItem)
            {
                //List<LootItemPositionClass> itemPositions = Traverse.Create(Singleton<GameWorld>.Instance).Field<List<LootItemPositionClass>>("list_1").Value;
                foreach (IKillableLootItem lootItem in Singleton<GameWorld>.Instance.LootList)
                {
                    if (lootItem is LootItem observedLootItem)
                    {
                        if (observedLootItem.Item.TemplateId == itemId && observedLootItem.isActiveAndEnabled)
                        {
                            return observedLootItem.Item;
                        }
                    }
                }
                FikaPlugin.Instance.FikaLogger.LogInfo($"CoopPlayer::FindItem: Could not find questItem with id '{itemId}' in the current session, either the quest is not active or something else occured.");
                return null;
            }

            Item item = Inventory.Equipment.FindItem(itemId);
            if (item != null)
            {
                return item;
            }

            GStruct416<Item> itemResult = FindItemById(itemId);
            if (itemResult.Error != null)
            {
                FikaPlugin.Instance.FikaLogger.LogError($"CoopPlayer::FindItem: Could not find item with id '{itemId}' in the world at all.");
            }
            return itemResult.Value;
        }

        #region handlers
        private class KeyHandler(CoopPlayer player)
        {
            private readonly CoopPlayer player = player;
            public GStruct416<KeyInteractionResultClass> unlockResult;

            internal void HandleKeyEvent()
            {
                unlockResult.Value.RaiseEvents(player._inventoryController, CommandStatus.Succeed);
            }
        }

        private class InventoryOperationHandler(GStruct411 opResult)
        {
            public readonly GStruct411 opResult = opResult;

            internal void HandleResult(IResult result)
            {
                if (!result.Succeed || !string.IsNullOrEmpty(result.Error))
                {
                    FikaPlugin.Instance.FikaLogger.LogError($"Error in operation: {result.Error}");
                }
            }
        }

        private class LootableContainerInteractionHandler(CoopPlayer player, LootableContainer container)
        {
            private readonly CoopPlayer player = player;
            public readonly LootableContainer container = container;

            public void Handle()
            {
                player.PacketSender.CommonPlayerPackets.Enqueue(new()
                {
                    HasContainerInteractionPacket = true,
                    ContainerInteractionPacket = new()
                    {
                        InteractiveId = container.Id,
                        InteractionType = EInteractionType.Close
                    }
                });

                container.Interact(new InteractionResult(EInteractionType.Close));
                if (player.MovementContext.LevelOnApproachStart > 0f)
                {
                    player.MovementContext.SetPoseLevel(player.MovementContext.LevelOnApproachStart, false);
                    player.MovementContext.LevelOnApproachStart = -1f;
                }
            }
        }

        private class FirearmControllerHandler(CoopPlayer coopPlayer, Weapon weapon)
        {
            private readonly CoopPlayer coopPlayer = coopPlayer;
            public readonly Weapon weapon = weapon;
            public Process<FirearmController, IFirearmHandsController> process;
            public Action confirmCallback;

            internal CoopClientFirearmController ReturnController()
            {
                return CoopClientFirearmController.Create(coopPlayer, weapon);
            }

            internal void SendPacket()
            {
                if (weapon.IsStationaryWeapon)
                {
                    return;
                }

                coopPlayer.PacketSender.CommonPlayerPackets.Enqueue(new()
                {
                    HasProceedPacket = true,
                    ProceedPacket = new()
                    {
                        ProceedType = EProceedType.Weapon,
                        ItemId = weapon.Id,
                        ItemTemplateId = weapon.TemplateId
                    }
                });
            }

            internal void HandleResult(IResult result)
            {
                if (result.Succeed)
                {
                    confirmCallback();
                }
            }
        }

        private class QuickUseItemControllerHandler(CoopPlayer coopPlayer, Item item)
        {
            private readonly CoopPlayer coopPlayer = coopPlayer;
            private readonly Item item = item;
            public Process<QuickUseItemController, IOnHandsUseCallback> process;
            public Action confirmCallback;

            internal QuickUseItemController ReturnController()
            {
                return QuickUseItemController.smethod_5<QuickUseItemController>(coopPlayer, item);
            }

            internal void SendPacket()
            {
                coopPlayer.PacketSender.CommonPlayerPackets.Enqueue(new()
                {
                    HasProceedPacket = true,
                    ProceedPacket = new()
                    {
                        ProceedType = EProceedType.QuickUse,
                        ItemId = item.Id,
                        ItemTemplateId = item.TemplateId
                    }
                });
            }

            internal void HandleResult(IResult result)
            {
                if (result.Succeed)
                {
                    confirmCallback();
                }
            }
        }

        private class MedsControllerHandler(CoopPlayer coopPlayer, MedsClass meds, EBodyPart bodyPart, int animationVariant)
        {
            private readonly CoopPlayer coopPlayer = coopPlayer;
            private readonly MedsClass meds = meds;
            private readonly EBodyPart bodyPart = bodyPart;
            private readonly int animationVariant = animationVariant;
            public Process<MedsController, GInterface142> process;
            public Action confirmCallback;

            internal MedsController ReturnController()
            {
                return MedsController.smethod_5<MedsController>(coopPlayer, meds, bodyPart, 1f, animationVariant);
            }

            internal void SendPacket()
            {
                coopPlayer.PacketSender.CommonPlayerPackets.Enqueue(new()
                {
                    HasProceedPacket = true,
                    ProceedPacket = new()
                    {
                        ProceedType = EProceedType.MedsClass,
                        ItemId = meds.Id,
                        ItemTemplateId = meds.TemplateId,
                        AnimationVariant = animationVariant,
                        BodyPart = bodyPart
                    }
                });
            }

            internal void HandleResult(IResult result)
            {
                if (result.Succeed)
                {
                    confirmCallback();
                }
            }
        }

        private class FoodControllerHandler(CoopPlayer coopPlayer, FoodClass foodDrink, float amount, EBodyPart bodyPart, int animationVariant)
        {
            private readonly CoopPlayer coopPlayer = coopPlayer;
            private readonly FoodClass foodDrink = foodDrink;
            private readonly float amount = amount;
            private readonly EBodyPart bodyPart = bodyPart;
            private readonly int animationVariant = animationVariant;
            public Process<MedsController, GInterface142> process;
            public Action confirmCallback;

            internal MedsController ReturnController()
            {
                return MedsController.smethod_5<MedsController>(coopPlayer, foodDrink, EBodyPart.Head, amount, animationVariant);
            }

            internal void SendPacket()
            {
                coopPlayer.PacketSender.CommonPlayerPackets.Enqueue(new()
                {
                    HasProceedPacket = true,
                    ProceedPacket = new()
                    {
                        ProceedType = EProceedType.MedsClass,
                        ItemId = foodDrink.Id,
                        Amount = amount,
                        ItemTemplateId = foodDrink.TemplateId,
                        AnimationVariant = animationVariant,
                        BodyPart = bodyPart
                    }
                });
            }

            internal void HandleResult(IResult result)
            {
                if (result.Succeed)
                {
                    confirmCallback();
                }
            }
        }

        private class KnifeControllerHandler(CoopPlayer coopPlayer, KnifeComponent knife)
        {
            private readonly CoopPlayer coopPlayer = coopPlayer;
            public readonly KnifeComponent knife = knife;
            public Process<KnifeController, IKnifeController> process;
            public Action confirmCallback;

            internal CoopClientKnifeController ReturnController()
            {
                return CoopClientKnifeController.Create(coopPlayer, knife);
            }

            internal void SendPacket()
            {
                coopPlayer.PacketSender.CommonPlayerPackets.Enqueue(new()
                {
                    HasProceedPacket = true,
                    ProceedPacket = new()
                    {
                        ProceedType = EProceedType.Knife,
                        ItemId = knife.Item.Id,
                        ItemTemplateId = knife.Item.TemplateId
                    }
                });
            }

            internal void HandleResult(IResult result)
            {
                if (result.Succeed)
                {
                    confirmCallback();
                }
            }
        }

        private class QuickKnifeControllerHandler(CoopPlayer coopPlayer, KnifeComponent knife)
        {
            private readonly CoopPlayer coopPlayer = coopPlayer;
            public readonly KnifeComponent knife = knife;
            public Process<QuickKnifeKickController, GInterface146> process;
            public Action confirmCallback;

            internal QuickKnifeKickController ReturnController()
            {
                return QuickKnifeKickController.smethod_8<QuickKnifeKickController>(coopPlayer, knife);
            }

            internal void SendPacket()
            {
                coopPlayer.PacketSender.CommonPlayerPackets.Enqueue(new()
                {
                    HasProceedPacket = true,
                    ProceedPacket = new()
                    {
                        ProceedType = EProceedType.QuickKnifeKick,
                        ItemId = knife.Item.Id,
                        ItemTemplateId = knife.Item.TemplateId
                    }
                });
            }

            internal void HandleResult(IResult result)
            {
                if (result.Succeed)
                {
                    confirmCallback();
                }
            }
        }

        private class GrenadeControllerHandler(CoopPlayer coopPlayer, GrenadeClass throwWeap)
        {
            private readonly CoopPlayer coopPlayer = coopPlayer;
            private readonly GrenadeClass throwWeap = throwWeap;
            public Process<GrenadeController, IHandsThrowController> process;
            public Action confirmCallback;

            internal CoopClientGrenadeController ReturnController()
            {
                return CoopClientGrenadeController.Create(coopPlayer, throwWeap);
            }

            internal void SendPacket()
            {
                coopPlayer.PacketSender.CommonPlayerPackets.Enqueue(new()
                {
                    HasProceedPacket = true,
                    ProceedPacket = new()
                    {
                        ProceedType = EProceedType.GrenadeClass,
                        ItemId = throwWeap.Id,
                        ItemTemplateId = throwWeap.TemplateId
                    }
                });
            }

            internal void HandleResult(IResult result)
            {
                if (result.Succeed)
                {
                    confirmCallback();
                }
            }
        }

        private class QuickGrenadeControllerHandler(CoopPlayer coopPlayer, GrenadeClass throwWeap)
        {
            private readonly CoopPlayer coopPlayer = coopPlayer;
            private readonly GrenadeClass throwWeap = throwWeap;
            public Process<QuickGrenadeThrowController, GInterface145> process;
            public Action confirmCallback;

            internal CoopClientQuickGrenadeController ReturnController()
            {
                return CoopClientQuickGrenadeController.Create(coopPlayer, throwWeap);
            }

            internal void SendPacket()
            {
                coopPlayer.PacketSender.CommonPlayerPackets.Enqueue(new()
                {
                    HasProceedPacket = true,
                    ProceedPacket = new()
                    {
                        ProceedType = EProceedType.QuickGrenadeThrow,
                        ItemId = throwWeap.Id,
                        ItemTemplateId = throwWeap.TemplateId
                    }
                });
            }

            internal void HandleResult(IResult result)
            {
                if (result.Succeed)
                {
                    confirmCallback();
                }
            }
        }

    }
    #endregion
}
