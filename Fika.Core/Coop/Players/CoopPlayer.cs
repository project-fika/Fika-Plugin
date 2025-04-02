// © 2025 Lacyway All Rights Reserved

using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.Communications;
using EFT.HealthSystem;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.SynchronizableObjects;
using EFT.UI;
using EFT.Vehicle;
using Fika.Core.Coop.ClientClasses;
using Fika.Core.Coop.ClientClasses.HandsControllers;
using Fika.Core.Coop.HostClasses;
using Fika.Core.Coop.ObservedClasses.Snapshotting;
using Fika.Core.Coop.PacketHandlers;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.Http;
using Fika.Core.Networking.VOIP;
using HarmonyLib;
using JsonType;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using static Fika.Core.Coop.ClientClasses.CoopClientInventoryController;
using static Fika.Core.Networking.CommonSubPackets;
using static Fika.Core.Networking.FirearmSubPackets;
using static Fika.Core.Networking.SubPacket;
using static Fika.Core.Networking.SubPackets;

namespace Fika.Core.Coop.Players
{
    /// <summary>
    /// <see cref="CoopPlayer"/> is the <see cref="LocalPlayer"/>, there can only be one <see cref="CoopPlayer"/> in every game and that is always yourself.
    /// </summary>
    public class CoopPlayer : LocalPlayer
    {
        #region Fields and Properties
        public IPacketSender PacketSender;
        public bool HasSkilledScav;
        public float ObservedOverlap = 0f;
        public CorpseSyncPacket CorpseSyncPacket = default;
        public bool HasGround;
        public int NetId;
        public bool IsObservedAI;
        public Dictionary<uint, Action<ServerOperationStatus>> OperationCallbacks = [];
        public FikaSnapshotter Snapshotter;
        public bool WaitingForCallback
        {
            get
            {
                return OperationCallbacks.Count > 0;
            }
        }

        protected string lastWeaponId;
        private bool shouldSendSideEffect;
        private GClass2042 voipHandler;
        private FikaVOIPController voipController;

        public ClientMovementContext ClientMovementContext
        {
            get
            {
                return MovementContext as ClientMovementContext;
            }
        }
        public Transform SpectateTransform
        {
            get
            {
                return PlayerBones.LootRaycastOrigin;
            }
        }
        public virtual bool LeftStanceDisabled { get; internal set; }
        public DateTime TalkDateTime { get; internal set; }

        #endregion

        public static async Task<CoopPlayer> Create(GameWorld gameWorld, int playerId, Vector3 position,
            Quaternion rotation, string layerName, string prefix, EPointOfView pointOfView, Profile profile,
            bool aiControl, EUpdateQueue updateQueue, EUpdateMode armsUpdateMode, EUpdateMode bodyUpdateMode,
            CharacterControllerSpawner.Mode characterControllerMode, Func<float> getSensitivity,
            Func<float> getAimingSensitivity, IStatisticsManager statisticsManager, IViewFilter filter, ISession session,
            int netId)
        {
            bool useSimpleAnimator = profile.Info.Settings.UseSimpleAnimator;
            ResourceKey resourceKey = useSimpleAnimator ? ResourceKeyManagerAbstractClass.ZOMBIE_BUNDLE_NAME : ResourceKeyManagerAbstractClass.PLAYER_BUNDLE_NAME;
            CoopPlayer player = Create<CoopPlayer>(gameWorld, resourceKey, playerId, position, updateQueue, armsUpdateMode,
                        bodyUpdateMode, characterControllerMode, getSensitivity, getAimingSensitivity, prefix, false, useSimpleAnimator);

            player.IsYourPlayer = true;
            player.NetId = netId;
            player.voipHandler = GClass2042.Default;

            PlayerOwnerInventoryController inventoryController = FikaBackendUtils.IsServer ? new CoopHostInventoryController(player, profile, false)
                : new CoopClientInventoryController(player, profile, false);

            LocalQuestControllerClass questController;
            if (FikaPlugin.Instance.SharedQuestProgression)
            {
                questController = new CoopClientSharedQuestController(profile, inventoryController, inventoryController.PlayerSearchController, session, player);
            }
            else
            {
                questController = new GClass3702(profile, inventoryController, inventoryController.PlayerSearchController, session);
            }
            questController.Init();
            GClass3706 achievementsController = new(profile, inventoryController, questController.Quests, session, true);
            achievementsController.Init();
            achievementsController.AchievementUnlocked += player.UnlockAchievement;
            achievementsController.Run();
            questController.Run();
            GClass3698 prestigeController = new(profile, inventoryController, questController.Quests, session);

            if (FikaBackendUtils.IsServer)
            {
                if (FikaBackendUtils.IsHeadless)
                {
                    player.PacketSender = HeadlessPacketSender.Create(player);
                }
                else
                {
                    player.PacketSender = ServerPacketSender.Create(player);
                }
            }
            else if (FikaBackendUtils.IsClient)
            {
                player.PacketSender = ClientPacketSender.Create(player);
            }

            EVoipState voipState = (!FikaBackendUtils.IsHeadless && Singleton<IFikaNetworkManager>.Instance.AllowVOIP && GClass1050.CheckMicrophone()) ? EVoipState.Available : EVoipState.NotAvailable;

            await player.Init(rotation, layerName, pointOfView, profile, inventoryController,
                new CoopClientHealthController(profile.Health, player, inventoryController, profile.Skills, aiControl),
                statisticsManager, questController, achievementsController, prestigeController, filter,
                voipState, false, false);

            foreach (MagazineItemClass magazineClass in player.Inventory.GetPlayerItems(EPlayerItems.NonQuestItems).OfType<MagazineItemClass>())
            {
                player.InventoryController.StrictCheckMagazine(magazineClass, true, player.Profile.MagDrillsMastering, false, false);
            }

            HashSet<ETraderServiceType> services = Traverse.Create(player).Field<HashSet<ETraderServiceType>>("hashSet_0").Value;
            foreach (ETraderServiceType etraderServiceType in Singleton<BackendConfigSettingsClass>.Instance.ServicesData.Keys)
            {
                services.Add(etraderServiceType);
            }

            player._handsController = EmptyHandsController.smethod_6<EmptyHandsController>(player);
            player._handsController.Spawn(1f, Class1655.class1655_0.method_0);
            player.AIData = new GClass567(null, player);
            player.AggressorFound = false;
            player._animators[0].enabled = true;

            RadioTransmitterRecodableComponent radioTransmitterRecodableComponent = player.FindRadioTransmitter();
            if (radioTransmitterRecodableComponent != null)
            {
                //Todo: (Archangel) method_131 refers to 'singlePlayerInventoryController_0' which is null in our case
                //radioTransmitterRecodableComponent.OnRadioTransmitterStatusChanged += player.method_131;

                if (player.Profile.GetTraderStanding("638f541a29ffd1183d187f57").IsZero())
                {
                    radioTransmitterRecodableComponent.SetEncoded(false);
                }
            }

            player.Profile.Info.SetProfileNickname(FikaBackendUtils.PMCName ?? profile.Nickname);

            return player;
        }

        public void AbuseNotification(string reporterId)
        {
            if (IsYourPlayer)
            {
                voipController?.ReceiveAbuseNotification(reporterId);
            }
        }

        private void UnlockAchievement(string tpl)
        {
            _achievementsController.UnlockAchievementForced(tpl);
        }

        public override void InitVoip(EVoipState voipState)
        {
            if (voipHandler.VoipEnabled && voipState != EVoipState.NotAvailable)
            {
                GClass1050 settings = Singleton<SharedGameSettingsClass>.Instance.Sound.Settings;
                if (!settings.VoipEnabled)
                {
                    voipState = EVoipState.Off;
                }
                if (!voipHandler.MicrophoneChecked)
                {
                    voipState = EVoipState.MicrophoneFail;
                }
                base.InitVoip(voipState);
                voipController = new(this, settings);
                VoipController = voipController;
            }
        }

        public override void CreateNestedSource()
        {
            base.CreateNestedSource();
            NestedStepSoundSource.SetBaseVolume(0.9f);
        }

        public override void CreateMovementContext()
        {
            LayerMask movement_MASK = EFTHardSettings.Instance.MOVEMENT_MASK;
            if (FikaPlugin.Instance.UseInertia)
            {
                MovementContext = ClientMovementContext.Create(this, GetBodyAnimatorCommon,
                    GetCharacterControllerCommon, movement_MASK);
                return;
            }
            MovementContext = NoInertiaMovementContext.Create(this, GetBodyAnimatorCommon,
                    GetCharacterControllerCommon, movement_MASK);
        }

        public override void OnSkillLevelChanged(AbstractSkillClass skill)
        {
            NotificationManagerClass.DisplayNotification(new GClass2312(skill));
        }

        public override void SendVoiceMuffledState(bool isMuffled)
        {
            GenericPacket packet = new()
            {
                NetId = NetId,
                Type = EGenericSubPacketType.MuffledState,
                SubPacket = new GenericSubPackets.MuffledState(NetId, isMuffled)
            };
            PacketSender.SendPacket(ref packet);
        }

        public override bool CheckSurface(float range)
        {
            HasGround = base.CheckSurface(range);
            return HasGround;
        }

        public override void OnWeaponMastered(MasterSkillClass masterSkill)
        {
            NotificationManagerClass.DisplayMessageNotification(string.Format("MasteringLevelUpMessage".Localized(null),
                masterSkill.MasteringGroup.Id.Localized(null),
                masterSkill.Level.ToString()), ENotificationDurationType.Default, ENotificationIconType.Default, null);
        }

        public override void ApplyDamageInfo(DamageInfoStruct damageInfo, EBodyPart bodyPartType, EBodyPartColliderType colliderType, float absorbed)
        {
            if (IsYourPlayer)
            {
                if (damageInfo.Player != null)
                {
                    if (!FikaPlugin.Instance.FriendlyFire && damageInfo.Player.iPlayer.GroupId == GroupId)
                    {
                        return;
                    }
                }
            }

            if (damageInfo.Weapon != null)
            {
                lastWeaponId = damageInfo.Weapon.Id;
            }

            base.ApplyDamageInfo(damageInfo, bodyPartType, colliderType, absorbed);
        }

        public override ShotInfoClass ApplyShot(DamageInfoStruct damageInfo, EBodyPart bodyPartType, EBodyPartColliderType colliderType, EArmorPlateCollider armorPlateCollider, ShotIdStruct shotId)
        {
            if (damageInfo.DamageType is EDamageType.Sniper or EDamageType.Landmine)
            {
                return SimulatedApplyShot(damageInfo, bodyPartType, colliderType, armorPlateCollider);
            }

            if (damageInfo.Player != null && damageInfo.Player.IsAI)
            {
                if (damageInfo.Weapon != null)
                {
                    lastWeaponId = damageInfo.Weapon.Id;
                }
                return SimulatedApplyShot(damageInfo, bodyPartType, colliderType, armorPlateCollider);
            }

            return null;
        }

        private ShotInfoClass SimulatedApplyShot(DamageInfoStruct damageInfo, EBodyPart bodyPartType, EBodyPartColliderType colliderType, EArmorPlateCollider armorPlateCollider)
        {
            ActiveHealthController activeHealthController = ActiveHealthController;
            if (activeHealthController != null && !activeHealthController.IsAlive)
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
            ApplyDamageInfo(damageInfo, bodyPartType, colliderType, 0f);
            ShotReactions(damageInfo, bodyPartType);
            ReceiveDamage(damageInfo.Damage, bodyPartType, damageInfo.DamageType, num, hitInfo.Material);

            return hitInfo;
        }

        #region Proceed
        public override void Proceed(bool withNetwork, Callback<GInterface171> callback, bool scheduled = true)
        {
            base.Proceed(withNetwork, callback, scheduled);
            CommonPlayerPacket packet = new()
            {
                NetId = NetId,
                Type = ECommonSubPacketType.Proceed,
                SubPacket = new ProceedPacket()
                {
                    ProceedType = EProceedType.EmptyHands,
                    Scheduled = scheduled
                }
            };
            PacketSender.SendPacket(ref packet);
        }

        public override void Proceed(FoodDrinkItemClass foodDrink, float amount, Callback<GInterface176> callback, int animationVariant, bool scheduled = true)
        {
            GStruct353<EBodyPart> bodyparts = new(EBodyPart.Head);
            FoodControllerHandler handler = new(this, foodDrink, amount, bodyparts, animationVariant);

            Func<MedsController> func = new(handler.ReturnController);
            handler.process = new(this, func, foodDrink, false);
            handler.confirmCallback = new(handler.SendPacket);
            handler.process.method_0(new(handler.HandleResult), callback, scheduled);
        }

        public override void Proceed(MedsItemClass meds, GStruct353<EBodyPart> bodyParts, Callback<GInterface176> callback, int animationVariant, bool scheduled = true)
        {
            MedsControllerHandler handler = new(this, meds, bodyParts, animationVariant);

            Func<MedsController> func = new(handler.ReturnController);
            handler.process = new(this, func, meds, false);
            handler.confirmCallback = new(handler.SendPacket);
            handler.process.method_0(new(handler.HandleResult), callback, scheduled);
        }

        public override void Proceed<T>(Item item, Callback<GInterface175> callback, bool scheduled = true)
        {
            if (item is PortableRangeFinderItemClass)
            {
                PortableRangeFinderControllerHandler rangeFinderHandler = new(this, item);

                Func<PortableRangeFinderController> rangeFinderFunc = new(rangeFinderHandler.ReturnController);
                rangeFinderHandler.process = new(this, rangeFinderFunc, item, false);
                rangeFinderHandler.confirmCallback = new(rangeFinderHandler.SendPacket);
                rangeFinderHandler.process.method_0(new(rangeFinderHandler.HandleResult), callback, scheduled);
                return;
            }

            UsableItemControllerHandler handler = new(this, item);

            Func<UsableItemController> func = new(handler.ReturnController);
            handler.process = new(this, func, item, false);
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

        public override void Proceed(KnifeComponent knife, Callback<GInterface180> callback, bool scheduled = true)
        {
            QuickKnifeControllerHandler handler = new(this, knife);

            Func<QuickKnifeKickController> func = new(handler.ReturnController);
            handler.process = new(this, func, handler.knife.Item, true);
            handler.confirmCallback = new(handler.SendPacket);
            handler.process.method_0(new(handler.HandleResult), callback, scheduled);
        }

        public override void Proceed(ThrowWeapItemClass throwWeap, Callback<GInterface179> callback, bool scheduled = true)
        {
            QuickGrenadeControllerHandler handler = new(this, throwWeap);

            Func<QuickGrenadeThrowHandsController> func = new(handler.ReturnController);
            handler.process = new(this, func, throwWeap, false);
            handler.confirmCallback = new(handler.SendPacket);
            handler.process.method_0(new(handler.HandleResult), callback, scheduled);
        }

        public override void Proceed(ThrowWeapItemClass throwWeap, Callback<IHandsThrowController> callback, bool scheduled = true)
        {
            GrenadeControllerHandler handler = new(this, throwWeap);

            Func<GrenadeHandsController> func = new(handler.ReturnController);
            handler.process = new(this, func, throwWeap, false);
            handler.confirmCallback = new(handler.SendPacket);
            handler.process.method_0(new(handler.HandleResult), callback, scheduled);
        }

        public override void Proceed(Weapon weapon, Callback<IFirearmHandsController> callback, bool scheduled = true)
        {
            FirearmControllerHandler handler = new(this, weapon);
            bool flag = false;
            if (_handsController is FirearmController firearmController)
            {
                flag = firearmController.CheckForFastWeaponSwitch(handler.weapon);
            }
            Func<FirearmController> func = new(handler.ReturnController);
            handler.process = new Process<FirearmController, IFirearmHandsController>(this, func, handler.weapon, flag);
            handler.confirmCallback = new(handler.SendPacket);
            handler.process.method_0(new(handler.HandleResult), callback, scheduled);
        }
        #endregion

        public override void DropCurrentController(Action callback, bool fastDrop, Item nextControllerItem = null)
        {
            CommonPlayerPacket packet = new()
            {
                NetId = NetId,
                Type = ECommonSubPacketType.Drop,
                SubPacket = new DropPacket()
                {
                    FastDrop = fastDrop
                }
            };
            PacketSender.SendPacket(ref packet);
            base.DropCurrentController(callback, fastDrop, nextControllerItem);
        }

        public override void OnBeenKilledByAggressor(IPlayer aggressor, DamageInfoStruct damageInfo, EBodyPart bodyPart, EDamageType lethalDamageType)
        {
            base.OnBeenKilledByAggressor(aggressor, damageInfo, bodyPart, lethalDamageType);

            // Handle 'Help Scav' rep gains
            if (aggressor is CoopPlayer coopPlayer)
            {
                if (coopPlayer.Side == EPlayerSide.Savage)
                {
                    coopPlayer.Loyalty.method_1(this);
                }

                if (Side == EPlayerSide.Savage && coopPlayer.Side != EPlayerSide.Savage && !coopPlayer.HasSkilledScav)
                {
                    coopPlayer.HasSkilledScav = true;
                    return;
                }
                else if (Side != EPlayerSide.Savage && HasSkilledScav && aggressor.Side == EPlayerSide.Savage)
                {
                    coopPlayer.Profile?.FenceInfo?.AddStanding(Profile.Info.Settings.StandingForKill, EFT.Counters.EFenceStandingSource.ScavHelp);
                }
            }
        }

        protected Item FindWeapon()
        {
#if DEBUG
            FikaPlugin.Instance.FikaLogger.LogWarning($"Finding weapon '{lastWeaponId}'!");
#endif
            GStruct457<Item> itemResult = FindItemById(lastWeaponId, false, false);
            Item item = itemResult.Value;
            if (!itemResult.Succeeded)
            {
                foreach (ThrowWeapItemClass grenadeClass in Singleton<IFikaNetworkManager>.Instance.CoopHandler.LocalGameInstance.ThrownGrenades)
                {
                    if (grenadeClass.Id == lastWeaponId)
                    {
                        item = grenadeClass;
                        break;
                    }
                }
            }

            if (item == null)
            {
                StationaryWeapon stationaryWeapon = GameWorld.FindStationaryWeaponByItemId(lastWeaponId);
                if (stationaryWeapon != null)
                {
                    item = stationaryWeapon.Item;
                }
            }

            return item;
        }

        protected void FindKillerWeapon()
        {
            if (LastAggressor == null)
            {
                FikaGlobals.LogWarning("LastAggressor was null, skipping");
                return;
            }

            if (!IsYourPlayer && LastAggressor.GroupId != "Fika")
            {
#if DEBUG
                FikaGlobals.LogWarning($"Skipping because {LastAggressor.Profile.Nickname} is not a player");
#endif
                return;
            }

            Item item = FindWeapon();
            if (item == null)
            {
                FikaGlobals.LogError($"Could not find killer weapon: {lastWeaponId}!");
                return;
            }
            LastDamageInfo.Weapon = item;
        }

        public void HandleTeammateKill(DamageInfoStruct damage, EBodyPart bodyPart,
            EPlayerSide playerSide, WildSpawnType role, string playerProfileId,
            float distance, List<string> targetEquipment,
            HealthEffects enemyEffects, List<string> zoneIds, CoopPlayer killer)
        {
            if (!HealthController.IsAlive)
            {
                return;
            }

#if DEBUG
            FikaPlugin.Instance.FikaLogger.LogWarning($"HandleTeammateKill: Weapon {(damage.Weapon != null ? damage.Weapon.Name.Localized() : "None")}");
#endif

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
                                bodyPart, Location, distance, role.ToStringNoBox(), CurrentHour, enemyEffects,
                                killer.HealthController.BodyPartEffects, zoneIds, killer.HealthController.ActiveBuffsNames());

                /*AbstractAchievementControllerClass.CheckKillConditionCounter(value, playerProfileId, targetEquipment, damage.Weapon,
                    bodyPart, Location, distance, role.ToStringNoBox(), hour, enemyEffects,
                    killer.HealthController.BodyPartEffects, zoneIds, killer.HealthController.ActiveBuffsNames());*/
            }
        }

        protected void HandleSharedExperience(bool countAsBoss, int experience, SessionCountersClass sessionCounters)
        {
            if (experience <= 0)
            {
                experience = Singleton<BackendConfigSettingsClass>.Instance.Experience.Kill.VictimBotLevelExp;
            }

            if (FikaPlugin.SharedKillExperience.Value && !countAsBoss)
            {
                int toReceive = experience / 2;
#if DEBUG
                FikaPlugin.Instance.FikaLogger.LogInfo($"Received shared kill XP of {toReceive}");
#endif
                sessionCounters.AddLong(1L, SessionCounterTypesAbstractClass.Kills);
                sessionCounters.AddInt(toReceive, SessionCounterTypesAbstractClass.ExpKillBase);
            }

            if (FikaPlugin.SharedBossExperience.Value && countAsBoss)
            {
                int toReceive = experience / 2;
#if DEBUG
                FikaPlugin.Instance.FikaLogger.LogInfo($"Received shared boss XP of {toReceive}");
#endif
                sessionCounters.AddLong(1L, SessionCounterTypesAbstractClass.Kills);
                sessionCounters.AddInt(toReceive, SessionCounterTypesAbstractClass.ExpKillBase);
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
            CommonPlayerPacket packet = new()
            {
                NetId = NetId,
                Type = ECommonSubPacketType.InventoryChanged,
                SubPacket = new InventoryChangedPacket()
                {
                    InventoryOpen = opened
                }
            };
            PacketSender.SendPacket(ref packet);
        }

        public override void SetCompassState(bool value)
        {
            base.SetCompassState(value);
            WeaponPacket packet = new()
            {
                NetId = NetId,
                Type = EFirearmSubPacketType.CompassChange,
                SubPacket = new CompassChangePacket()
                {
                    Enabled = value
                }
            };
            PacketSender.SendPacket(ref packet);
        }

        public override void SendHeadlightsPacket(bool isSilent)
        {
            if (PacketSender != null && PacketSender.Enabled)
            {
                FirearmLightStateStruct[] lightStates = _helmetLightControllers.Select(FikaGlobals.GetFirearmLightStates).ToArray();

                CommonPlayerPacket packet = new()
                {
                    NetId = NetId,
                    Type = ECommonSubPacketType.HeadLights,
                    SubPacket = new HeadLightsPacket()
                    {
                        Amount = lightStates.Count(),
                        IsSilent = isSilent,
                        LightStates = lightStates
                    }
                };
                PacketSender.SendPacket(ref packet);
            }
        }

        public override void SendWeaponLightPacket()
        {
            if (PacketSender == null)
            {
                return;
            }

            if (HandsController is CoopClientFirearmController controller)
            {
                FirearmLightStateStruct[] array = controller.Item.AllSlots.Select(FikaGlobals.GetContainedItem)
                    .GetComponents<LightComponent>().Select(FikaGlobals.GetFirearmLightStatesFromComponent)
                    .ToArray();

                if (array.Length == 0)
                {
                    return;
                }

                LightStatesPacket subPacket = new()
                {
                    Amount = array.Length,
                    States = array
                };

                for (int i = 0; i < array.Length; i++)
                {
                    FirearmLightStateStruct firearmLightStateStruct = array[i];
                    subPacket.States[i] = new FirearmLightStateStruct
                    {
                        Id = firearmLightStateStruct.Id,
                        IsActive = firearmLightStateStruct.IsActive,
                        LightMode = firearmLightStateStruct.LightMode
                    };
                }

                WeaponPacket packet = new()
                {
                    NetId = NetId,
                    Type = EFirearmSubPacketType.ToggleLightStates,
                    SubPacket = subPacket
                };

                PacketSender.SendPacket(ref packet);
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
                CommonPlayerPacket packet = new()
                {
                    NetId = NetId,
                    Type = ECommonSubPacketType.Phrase,
                    SubPacket = new PhrasePacket()
                    {
                        PhraseTrigger = @event,
                        PhraseIndex = clip.NetId
                    }
                };
                PacketSender.SendPacket(ref packet);
            }
        }

        public override Corpse CreateCorpse()
        {
            if (FikaBackendUtils.IsServer)
            {
                Corpse corpse = base.CreateCorpse();
                corpse.IsZombieCorpse = UsedSimplifiedSkeleton;
                //CorpsePositionSyncer.Create(corpse.gameObject, corpse, NetId);
                return corpse;
            }

            ObservedCorpse observedCorpse = CreateCorpse<ObservedCorpse>(CorpseSyncPacket.OverallVelocity);
            observedCorpse.IsZombieCorpse = UsedSimplifiedSkeleton;
            observedCorpse.SetSpecificSettings(PlayerBones.RightPalm);
            Singleton<GameWorld>.Instance.ObservedPlayersCorpses.Add(NetId, observedCorpse);
            return observedCorpse;
        }

        public override void OperateStationaryWeapon(StationaryWeapon stationaryWeapon, StationaryPacketStruct.EStationaryCommand command)
        {
            base.OperateStationaryWeapon(stationaryWeapon, command);
            CommonPlayerPacket packet = new()
            {
                NetId = NetId,
                Type = ECommonSubPacketType.Stationary,
                SubPacket = new StationaryPacket()
                {
                    Command = (EStationaryCommand)command,
                    Id = stationaryWeapon.Id
                }
            };
            PacketSender.SendPacket(ref packet);
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
                NetId = NetId,
                Type = ECommonSubPacketType.WorldInteraction,
                SubPacket = new WorldInteractionPacket()
                {
                    InteractiveId = interactiveObject.Id,
                    InteractionType = interactionResult.InteractionType,
                    InteractionStage = EInteractionStage.Start,
                    ItemId = (interactionResult is GClass3424 keyInteractionResult) ? keyInteractionResult.Key.Item.Id : string.Empty
                }
            };
            PacketSender.SendPacket(ref packet);
        }

        // Execute
        public override void vmethod_1(WorldInteractiveObject door, InteractionResult interactionResult)
        {
            if (door == null)
            {
                return;
            }

            if (this is ObservedCoopPlayer)
            {
                base.vmethod_1(door, interactionResult);
                return;
            }

            base.vmethod_1(door, interactionResult);

            if (!door.ForceLocalInteraction)
            {
                CommonPlayerPacket packet = new()
                {
                    NetId = NetId,
                    Type = ECommonSubPacketType.WorldInteraction,
                    SubPacket = new WorldInteractionPacket()
                    {
                        InteractiveId = door.Id,
                        InteractionType = interactionResult.InteractionType,
                        InteractionStage = EInteractionStage.Execute,
                        ItemId = (interactionResult is GClass3424 keyInteractionResult) ? keyInteractionResult.Key.Item.Id : string.Empty
                    }
                };
                PacketSender.SendPacket(ref packet);
            }

            UpdateInteractionCast();
        }

        public override void OnAnimatedInteraction(EInteraction interaction)
        {
            if (!FikaGlobals.BlockedInteractions.Contains(interaction))
            {
                CommonPlayerPacket packet = new()
                {
                    NetId = NetId,
                    Type = ECommonSubPacketType.Interaction,
                    SubPacket = new InteractionPacket()
                    {
                        Interaction = interaction
                    }
                };
                PacketSender.SendPacket(ref packet);
            }
        }

        public override void HealthControllerUpdate(float deltaTime)
        {
            _healthController.ManualUpdate(deltaTime);
        }

        public override void OnMounting(MountingPacketStruct.EMountingCommand command)
        {
            MountingPacket packet = new()
            {
                Command = command,
                IsMounted = MovementContext.IsInMountedState,
                MountDirection = MovementContext.IsInMountedState ? MovementContext.PlayerMountingPointData.MountPointData.MountDirection : default,
                MountingPoint = MovementContext.IsInMountedState ? MovementContext.PlayerMountingPointData.MountPointData.MountPoint : default,
                CurrentMountingPointVerticalOffset = MovementContext.IsInMountedState ? MovementContext.PlayerMountingPointData.CurrentMountingPointVerticalOffset : 0f,
                MountingDirection = MovementContext.IsInMountedState ? (short)MovementContext.PlayerMountingPointData.MountPointData.MountSideDirection : (short)0
            };
            if (command == MountingPacketStruct.EMountingCommand.Enter)
            {
                packet.TransitionTime = MovementContext.PlayerMountingPointData.CurrentApproachTime;
                packet.TargetPos = MovementContext.PlayerMountingPointData.PlayerTargetPos;
                packet.TargetPoseLevel = MovementContext.PlayerMountingPointData.TargetPoseLevel;
                packet.TargetHandsRotation = MovementContext.PlayerMountingPointData.TargetHandsRotation;
                packet.TargetBodyRotation = MovementContext.PlayerMountingPointData.TargetBodyRotation;
                packet.PoseLimit = MovementContext.PlayerMountingPointData.PoseLimit;
                packet.PitchLimit = MovementContext.PlayerMountingPointData.PitchLimit;
                packet.YawLimit = MovementContext.PlayerMountingPointData.YawLimit;
            }

            CommonPlayerPacket netPacket = new()
            {
                NetId = NetId,
                Type = ECommonSubPacketType.Mounting,
                SubPacket = packet
            };
            PacketSender.SendPacket(ref netPacket);
        }

        public override void vmethod_3(TransitControllerAbstractClass controller, int transitPointId, string keyId, EDateTime time)
        {
            TransitInteractionPacketStruct packet = controller.GetInteractPacket(transitPointId, keyId, time);
            if (FikaBackendUtils.IsServer)
            {
                controller.InteractWithTransit(this, packet);
            }
            else
            {
                TransitInteractPacket interactPacket = new()
                {
                    NetId = NetId,
                    Data = packet
                };
                Singleton<FikaClient>.Instance.SendData(ref interactPacket, LiteNetLib.DeliveryMethod.ReliableOrdered);
                if (Singleton<GameWorld>.Instance.TransitController is FikaClientTransitController transitController)
                {
                    transitController.InteractPacket = packet;
                }
            }
            UpdateInteractionCast();
        }

        public override void vmethod_4(TripwireSynchronizableObject tripwire)
        {
            base.vmethod_4(tripwire);
            AirplaneDataPacketStruct data = new()
            {
                ObjectType = SynchronizableObjectType.Tripwire,
                ObjectId = tripwire.ObjectId,
                PacketData = new()
                {
                    TripwireDataPacket = new()
                    {
                        State = ETripwireState.Inert
                    }
                },
                Position = tripwire.transform.position,
                Rotation = tripwire.transform.rotation.eulerAngles,
                IsActive = true
            };
            UpdateInteractionCast();

            GenericPacket packet = new()
            {
                NetId = NetId,
                Type = EGenericSubPacketType.DisarmTripwire,
                SubPacket = new GenericSubPackets.DisarmTripwire(data)
            };

            if (FikaBackendUtils.IsServer)
            {
                Singleton<FikaServer>.Instance.SendDataToAll(ref packet, LiteNetLib.DeliveryMethod.ReliableOrdered);
                return;
            }

            Singleton<FikaClient>.Instance.SendData(ref packet, LiteNetLib.DeliveryMethod.ReliableOrdered);
        }

        public override void ApplyCorpseImpulse()
        {
            Corpse.Ragdoll.ApplyImpulse(LastDamageInfo.HitCollider, LastDamageInfo.Direction, LastDamageInfo.HitPoint, _corpseAppliedForce);
        }

        public HealthSyncPacket SetupCorpseSyncPacket(NetworkHealthSyncPacketStruct packet)
        {
            float num = EFTHardSettings.Instance.HIT_FORCE;
            num *= 0.3f + 0.7f * Mathf.InverseLerp(50f, 20f, LastDamageInfo.PenetrationPower);
            _corpseAppliedForce = num;

            if (FikaBackendUtils.IsServer || IsYourPlayer)
            {
                if (Side is not EPlayerSide.Savage)
                {
                    GenerateDogtagDetails();
                }
            }

            GClass1693 inventoryDescriptor = EFTItemSerializerClass.SerializeItem(Inventory.Equipment, FikaGlobals.SearchControllerSerializer);

            HealthSyncPacket syncPacket = new()
            {
                NetId = NetId,
                Packet = packet,
                KillerId = LastAggressor != null ? LastAggressor.ProfileId : string.Empty,
                BodyPart = LastBodyPart,
                WeaponId = lastWeaponId,
                CorpseSyncPacket = new()
                {
                    BodyPartColliderType = LastDamageInfo.BodyPartColliderType,
                    Direction = LastDamageInfo.Direction,
                    Point = LastDamageInfo.HitPoint,
                    Force = _corpseAppliedForce,
                    OverallVelocity = Velocity,
                    InventoryDescriptor = inventoryDescriptor,
                    ItemSlot = EquipmentSlot.ArmBand
                },
                TriggerZones = TriggerZones.Count > 0 ? [.. TriggerZones] : null,
            };

            if (HandsController.Item != null)
            {
                Item heldItem = HandsController.Item;
                EquipmentSlot[] weaponSlots = [EquipmentSlot.FirstPrimaryWeapon, EquipmentSlot.SecondPrimaryWeapon, EquipmentSlot.Holster, EquipmentSlot.Scabbard];
                foreach (EquipmentSlot weaponSlot in weaponSlots)
                {
                    if (heldItem == Equipment.GetSlot(weaponSlot).ContainedItem)
                    {
                        syncPacket.CorpseSyncPacket.ItemSlot = weaponSlot;
                        break;
                    }
                }
            }

            return syncPacket;
        }

        public override void OnDead(EDamageType damageType)
        {
            if (LastDamageInfo.Weapon == null && !string.IsNullOrEmpty(lastWeaponId))
            {
                FindKillerWeapon();
#if DEBUG
                if (LastDamageInfo.Weapon != null)
                {
                    FikaPlugin.Instance.FikaLogger.LogWarning($"Found weapon '{LastDamageInfo.Weapon.Name.Localized()}'!");
                }
#endif
            }
            base.OnDead(damageType);
            PacketSender.Enabled = false;
            if (IsYourPlayer)
            {
                StartCoroutine(LocalPlayerDied());
            }
        }

        private void GenerateDogtagDetails()
        {
            if (LastDamageInfo.Weapon == null && !string.IsNullOrEmpty(lastWeaponId))
            {
                FindKillerWeapon();
#if DEBUG
                if (LastDamageInfo.Weapon != null)
                {
                    FikaPlugin.Instance.FikaLogger.LogWarning($"Found weapon '{LastDamageInfo.Weapon.Name.Localized()}'!");
                }
#endif
            }

            string accountId = AccountId;
            string profileId = ProfileId;
            string nickname = Profile.Nickname;
            bool hasAggressor = LastAggressor != null;
            string killerAccountId = hasAggressor ? LastAggressor.AccountId : string.Empty;
            string killerProfileId = hasAggressor ? LastAggressor.ProfileId : string.Empty;
            string killerNickname = (hasAggressor && !string.IsNullOrEmpty(LastAggressor.Profile.Nickname)) ? LastAggressor.Profile.Nickname : string.Empty;
            EPlayerSide side = Side;
            int level = Profile.Info.Level;
            DateTime time = EFTDateTimeClass.UtcNow.ToLocalTime();
            string weaponName = LastAggressor != null ? (LastDamageInfo.Weapon != null ? LastDamageInfo.Weapon.ShortName : string.Empty) : "-";
            string groupId = GroupId;

            Item item = Equipment.GetSlot(EquipmentSlot.Dogtag).ContainedItem;
            if (item != null)
            {
                DogtagComponent dogtagComponent = item.GetItemComponent<DogtagComponent>();
                if (dogtagComponent != null)
                {
                    dogtagComponent.Item.SpawnedInSession = true;
                    dogtagComponent.AccountId = accountId;
                    dogtagComponent.ProfileId = profileId;
                    dogtagComponent.Nickname = nickname;
                    dogtagComponent.KillerAccountId = killerAccountId;
                    dogtagComponent.KillerProfileId = killerProfileId;
                    dogtagComponent.KillerName = killerNickname;
                    dogtagComponent.Side = side;
                    dogtagComponent.Level = level;
                    dogtagComponent.Time = time;
                    dogtagComponent.Status = hasAggressor ? "Killed by" : "Died";
                    dogtagComponent.WeaponName = weaponName;
                    dogtagComponent.GroupId = groupId;
                    return;
                }
            }

            FikaPlugin.Instance.FikaLogger.LogError($"GenerateAndSendDogTagPacket: Item or Dogtagcomponent was null on player {Profile.Nickname}, id {NetId}");
        }

        private IEnumerator LocalPlayerDied()
        {
            AddPlayerRequest request = new(FikaBackendUtils.GroupId, ProfileId, FikaBackendUtils.IsSpectator);
            Task diedTask = FikaRequestHandler.PlayerDied(request);
            while (!diedTask.IsCompleted)
            {
                yield return new WaitForEndOfFrame();
            }
        }

        public void HandleInteractPacket(WorldInteractionPacket packet)
        {
            WorldInteractiveObject worldInteractiveObject = Singleton<GameWorld>.Instance.FindDoor(packet.InteractiveId);
            if (worldInteractiveObject != null)
            {
                if (worldInteractiveObject.isActiveAndEnabled && !worldInteractiveObject.ForceLocalInteraction)
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

                        GStruct457<Item> result = FindItemById(packet.ItemId, false, false);
                        if (!result.Succeeded)
                        {
                            FikaPlugin.Instance.FikaLogger.LogWarning("HandleInteractPacket: Could not find item: " + packet.ItemId);
                            return;
                        }

                        KeyComponent keyComponent = result.Value.GetItemComponent<KeyComponent>();
                        if (keyComponent == null)
                        {
                            FikaPlugin.Instance.FikaLogger.LogWarning("HandleInteractPacket: keyComponent was null!");
                            return;
                        }

                        keyHandler.unlockResult = worldInteractiveObject.UnlockOperation(keyComponent, this, worldInteractiveObject);
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

        public override void TryInteractionCallback(LootableContainer container)
        {
            LootableContainerInteractionHandler handler = new(this, container);
            if (handler.container != null && _openAction != null)
            {
                _openAction(handler.Handle);
            }
            _openAction = null;
        }

        public override void vmethod_2(BTRSide btr, byte placeId, EInteractionType interaction)
        {
            if (FikaBackendUtils.IsServer)
            {
                base.vmethod_2(btr, placeId, interaction);
                return;
            }

            FikaClient client = Singleton<FikaClient>.Instance;
            BTRInteractionPacket packet = new(NetId)
            {
                Data = btr.GetInteractWithBtrPacket(placeId, interaction)
            };
            client.SendData(ref packet, LiteNetLib.DeliveryMethod.ReliableOrdered);
        }

        public void SetupMainPlayer()
        {
            // Set own group id, set different group to headless client
            if (FikaBackendUtils.IsHeadless)
            {
                Profile.Info.GroupId = "HEADLESS";
                Profile.Info.GroupId = "HEADLESS";
            }
            else
            {
                Profile.Info.GroupId = "Fika";
                Profile.Info.TeamId = "Fika";
            }
        }

        public void HandleHeadLightsPacket(HeadLightsPacket packet)
        {
            try
            {
                if (_helmetLightControllers != null)
                {
                    for (int i = 0; i < _helmetLightControllers.Count(); i++)
                    {
                        _helmetLightControllers.ElementAt(i)?.LightMod?.SetLightState(packet.LightStates[i]);
                    }
                    if (!packet.IsSilent)
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

        public void HandleInventoryOpenedPacket(bool opened)
        {
            base.SetInventoryOpened(opened);
        }

        public void HandleDropPacket(bool fastDrop)
        {
            DropHandler handler = new(this);
            base.DropCurrentController(handler.HandleResult, fastDrop, null);
        }

        public void HandleUsableItemPacket(UsableItemPacket packet)
        {
            if (HandsController is UsableItemController usableItemController)
            {
                if (packet.ExamineWeapon)
                {
                    usableItemController.ExamineWeapon();
                }

                if (packet.HasCompassState)
                {
                    usableItemController.CompassState.Value = packet.CompassState;
                }

                if (packet.HasAim)
                {
                    usableItemController.IsAiming = packet.AimState;
                }
            }
        }

        public void ObservedStationaryInteract(StationaryWeapon stationaryWeapon, StationaryPacketStruct.EStationaryCommand command)
        {
            if (command == StationaryPacketStruct.EStationaryCommand.Occupy)
            {
                stationaryWeapon.SetOperator(ProfileId, false);
                MovementContext.StationaryWeapon = stationaryWeapon;
                MovementContext.InteractionParameters = stationaryWeapon.GetInteractionParameters();
                MovementContext.PlayerAnimatorSetApproached(false);
                MovementContext.PlayerAnimatorSetStationary(true);
                MovementContext.PlayerAnimatorSetStationaryAnimation((int)stationaryWeapon.Animation);
                return;
            }
            if (command == StationaryPacketStruct.EStationaryCommand.Leave)
            {
                return;
            }
            MovementContext.PlayerAnimatorSetStationary(false);
            if (MovementContext.StationaryWeapon != null)
            {
                MovementContext.StationaryWeapon.Unlock(ProfileId);
            }
        }

        public virtual void DoObservedVault(VaultPacket vaultPacket)
        {

        }

        public override void PauseAllEffectsOnPlayer()
        {
            ActiveHealthController.PauseAllEffects();
        }

        public override void UnpauseAllEffectsOnPlayer()
        {
            ActiveHealthController.UnpauseAllEffects();
        }

        public void HandleCallbackFromServer(OperationCallbackPacket operationCallbackPacket)
        {
            if (OperationCallbacks.TryGetValue(operationCallbackPacket.CallbackId, out Action<ServerOperationStatus> callback))
            {
                if (operationCallbackPacket.OperationStatus != EOperationStatus.Started)
                {
                    OperationCallbacks.Remove(operationCallbackPacket.CallbackId);
                }
                ServerOperationStatus status = new(operationCallbackPacket.OperationStatus, operationCallbackPacket.Error);
                callback(status);
            }
            else
            {
                FikaPlugin.Instance.FikaLogger.LogError($"Could not find CallbackId: {operationCallbackPacket.CallbackId}!");
            }
        }

        public override void ApplyExplosionDamageToArmor(Dictionary<ExplosiveHitArmorColliderStruct, float> armorDamage, DamageInfoStruct damageInfo)
        {
            if (IsYourPlayer)
            {
                _preAllocatedArmorComponents.Clear();
                List<ArmorComponent> listTocheck = [];
                Inventory.GetPutOnArmorsNonAlloc(listTocheck);
                foreach (ArmorComponent armorComponent in listTocheck)
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
                        num = armorComponent.ApplyExplosionDurabilityDamage(num, damageInfo, _preAllocatedArmorComponents);
                        method_92(num, armorComponent);
                    }
                }

                if (_preAllocatedArmorComponents.Count > 0)
                {
                    SendArmorDamagePacket();
                }
            }
        }

        public void SendArmorDamagePacket()
        {
            int amount = _preAllocatedArmorComponents.Count;
            if (amount > 0)
            {
                string[] ids = new string[amount];
                float[] durabilities = new float[amount];

                for (int i = 0; i < amount; i++)
                {
                    ids[i] = _preAllocatedArmorComponents[i].Item.Id;
                    durabilities[i] = _preAllocatedArmorComponents[i].Repairable.Durability;
                }

                ArmorDamagePacket packet = new()
                {
                    NetId = NetId,
                    ItemIds = ids,
                    Durabilities = durabilities,
                };
                PacketSender.SendPacket(ref packet);
            }
        }

        public virtual void HandleDamagePacket(DamagePacket packet)
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
                        if (!FikaPlugin.Instance.FriendlyFire && damageInfo.Player.iPlayer.GroupId == GroupId)
                        {
                            return;
                        }
                    }
                }
                lastWeaponId = packet.WeaponId;
            }

            if (damageInfo.DamageType == EDamageType.Melee && !string.IsNullOrEmpty(lastWeaponId))
            {
                Item item = FindWeapon();
                if (item != null)
                {
                    damageInfo.Weapon = item;
                    (damageInfo.Player.iPlayer as CoopPlayer).shouldSendSideEffect = true;
#if DEBUG
                    FikaPlugin.Instance.FikaLogger.LogWarning("Found weapon for knife damage: " + item.Name.Localized());
#endif
                }
            }

            ShotReactions(damageInfo, packet.BodyPartType);
            ReceiveDamage(damageInfo.Damage, packet.BodyPartType, damageInfo.DamageType, packet.Absorbed, packet.Material);
            base.ApplyDamageInfo(damageInfo, packet.BodyPartType, packet.ColliderType, packet.Absorbed);
        }

        public override void OnSideEffectApplied(SideEffectComponent sideEffect)
        {
            if (!shouldSendSideEffect)
            {
                return;
            }

            SideEffectPacket packet = new()
            {
                ItemId = sideEffect.Item.Id,
                Value = sideEffect.Value
            };

            PacketSender.SendPacket(ref packet, true);
            shouldSendSideEffect = false;
        }

        public void HandleArmorDamagePacket(ArmorDamagePacket packet)
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
                GStruct457<Item> gstruct = Singleton<GameWorld>.Instance.FindItemById(packet.ItemIds[i]);
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

        public override void UpdateTick()
        {
            voipController?.Update();
            base.UpdateTick();
        }

        public void CheckAndResetControllers(ExitStatus exitStatus, float pastTime, string locationId, string exitName)
        {
            _questController?.CheckExitConditionCounters(exitStatus, pastTime, locationId, exitName, HealthController.BodyPartEffects, TriggerZones);
            _questController?.ResetCurrentNullableCounters();

            _achievementsController?.CheckExitConditionCounters(exitStatus, pastTime, locationId, exitName, HealthController.BodyPartEffects, TriggerZones);
            _achievementsController?.ResetCurrentNullableCounters();
        }

        public override void Dispose()
        {
            if (PacketSender != null)
            {
                PacketSender.Enabled = false;
                PacketSender.DestroyThis();
                PacketSender = null;
            }
            voipController?.Dispose();
            base.Dispose();
        }

        public override void OnGameSessionEnd(ExitStatus exitStatus, float pastTime, string locationId, string exitName)
        {
            if (_achievementsController != null)
            {
                _achievementsController.AchievementUnlocked -= UnlockAchievement;
            }
            base.OnGameSessionEnd(exitStatus, pastTime, locationId, exitName);
        }

        public override void OnVaulting()
        {
            CommonPlayerPacket packet = new()
            {
                NetId = NetId,
                Type = ECommonSubPacketType.Vault,
                SubPacket = new VaultPacket()
                {
                    VaultingStrategy = VaultingParameters.GetVaultingStrategy(),
                    VaultingPoint = VaultingParameters.MaxWeightPointPosition,
                    VaultingHeight = VaultingParameters.VaultingHeight,
                    VaultingLength = VaultingParameters.VaultingLength,
                    VaultingSpeed = MovementContext.VaultingSpeed,
                    BehindObstacleHeight = VaultingParameters.BehindObstacleRatio,
                    AbsoluteForwardVelocity = VaultingParameters.AbsoluteForwardVelocity
                }
            };
            PacketSender.SendPacket(ref packet);
        }

        public void ReceiveTraderServicesData(List<TraderServicesClass> services)
        {
            if (!IsYourPlayer)
            {
                return;
            }

            Dictionary<ETraderServiceType, BackendConfigSettingsClass.ServiceData> servicesData = Singleton<BackendConfigSettingsClass>.Instance.ServicesData;

            foreach (TraderServicesClass service in services)
            {
                BackendConfigSettingsClass.ServiceData serviceData = new(service, null);
                if (servicesData.ContainsKey(serviceData.ServiceType))
                {
                    servicesData[serviceData.ServiceType] = serviceData;
                }
                else
                {
                    servicesData.Add(serviceData.ServiceType, serviceData);
                }
                if (!Profile.TradersInfo.TryGetValue(serviceData.TraderId, out Profile.TraderInfo traderInfo))
                {
                    FikaPlugin.Instance.FikaLogger.LogWarning($"Can't find trader with id: {serviceData.TraderId}!");
                }
                else
                {
                    traderInfo.SetServiceAvailability(serviceData.ServiceType, service.CanAfford, service.WasPurchasedInThisRaid);
                }
            }
        }

        public Item FindQuestItem(string itemId)
        {
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
#if DEBUG
            FikaPlugin.Instance.FikaLogger.LogInfo($"CoopPlayer::FindItem: Could not find questItem with id '{itemId}' in the current session, either the quest is not active or something else occured.");
#endif
            return null;
        }

        #region handlers
        public class KeyHandler(CoopPlayer player)
        {
            private readonly CoopPlayer player = player;
            public GStruct457<GClass3424> unlockResult;

            internal void HandleKeyEvent()
            {
                unlockResult.Value.RaiseEvents(player._inventoryController, CommandStatus.Succeed);
            }
        }

        private class LootableContainerInteractionHandler(CoopPlayer player, LootableContainer container)
        {
            private readonly CoopPlayer player = player;
            public readonly LootableContainer container = container;

            public void Handle()
            {
                CommonPlayerPacket packet = new()
                {
                    NetId = player.NetId,
                    Type = ECommonSubPacketType.ContainerInteraction,
                    SubPacket = new ContainerInteractionPacket()
                    {
                        InteractiveId = container.Id,
                        InteractionType = EInteractionType.Close
                    }
                };
                player.PacketSender.SendPacket(ref packet);

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
                CommonPlayerPacket packet = new()
                {
                    NetId = coopPlayer.NetId,
                    Type = ECommonSubPacketType.Proceed,
                    SubPacket = new ProceedPacket()
                    {
                        ProceedType = weapon.IsStationaryWeapon ? EProceedType.Stationary : EProceedType.Weapon,
                        ItemId = weapon.Id
                    }
                };
                coopPlayer.PacketSender.SendPacket(ref packet);
            }

            internal void HandleResult(IResult result)
            {
                if (result.Succeed)
                {
                    confirmCallback();
                }
            }
        }

        private class UsableItemControllerHandler(CoopPlayer coopPlayer, Item item)
        {
            private readonly CoopPlayer coopPlayer = coopPlayer;
            private readonly Item item = item;
            public Process<UsableItemController, GInterface175> process;
            public Action confirmCallback;

            internal CoopClientUsableItemController ReturnController()
            {
                return CoopClientUsableItemController.Create(coopPlayer, item);
            }

            internal void SendPacket()
            {
                CommonPlayerPacket packet = new()
                {
                    NetId = coopPlayer.NetId,
                    Type = ECommonSubPacketType.Proceed,
                    SubPacket = new ProceedPacket()
                    {
                        ProceedType = EProceedType.UsableItem,
                        ItemId = item.Id
                    }
                };
                coopPlayer.PacketSender.SendPacket(ref packet);
            }

            internal void HandleResult(IResult result)
            {
                if (result.Succeed)
                {
                    confirmCallback();
                }
            }
        }

        private class PortableRangeFinderControllerHandler(CoopPlayer coopPlayer, Item item)
        {
            private readonly CoopPlayer coopPlayer = coopPlayer;
            private readonly Item item = item;
            public Process<PortableRangeFinderController, GInterface175> process;
            public Action confirmCallback;

            internal CoopClientPortableRangeFinderController ReturnController()
            {
                return CoopClientPortableRangeFinderController.Create(coopPlayer, item);
            }

            internal void SendPacket()
            {
                CommonPlayerPacket packet = new()
                {
                    NetId = coopPlayer.NetId,
                    Type = ECommonSubPacketType.Proceed,
                    SubPacket = new ProceedPacket()
                    {
                        ProceedType = EProceedType.UsableItem,
                        ItemId = item.Id
                    }
                };
                coopPlayer.PacketSender.SendPacket(ref packet);
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
                return QuickUseItemController.smethod_6<QuickUseItemController>(coopPlayer, item);
            }

            internal void SendPacket()
            {
                CommonPlayerPacket packet = new()
                {
                    NetId = coopPlayer.NetId,
                    Type = ECommonSubPacketType.Proceed,
                    SubPacket = new ProceedPacket()
                    {
                        ProceedType = EProceedType.QuickUse,
                        ItemId = item.Id
                    }
                };
                coopPlayer.PacketSender.SendPacket(ref packet);
            }

            internal void HandleResult(IResult result)
            {
                if (result.Succeed)
                {
                    confirmCallback();
                }
            }
        }

        private class MedsControllerHandler(CoopPlayer coopPlayer, MedsItemClass meds, GStruct353<EBodyPart> bodyParts, int animationVariant)
        {
            private readonly CoopPlayer coopPlayer = coopPlayer;
            private readonly MedsItemClass meds = meds;
            private readonly GStruct353<EBodyPart> bodyParts = bodyParts;
            private readonly int animationVariant = animationVariant;
            public Process<MedsController, GInterface176> process;
            public Action confirmCallback;

            internal MedsController ReturnController()
            {
                return MedsController.smethod_6<MedsController>(coopPlayer, meds, bodyParts, 1f, animationVariant);
            }

            internal void SendPacket()
            {
                CommonPlayerPacket packet = new()
                {
                    NetId = coopPlayer.NetId,
                    Type = ECommonSubPacketType.Proceed,
                    SubPacket = new ProceedPacket()
                    {
                        ProceedType = EProceedType.MedsClass,
                        ItemId = meds.Id,
                        AnimationVariant = animationVariant,
                        BodyParts = bodyParts
                    }
                };
                coopPlayer.PacketSender.SendPacket(ref packet);
            }

            internal void HandleResult(IResult result)
            {
                if (result.Succeed)
                {
                    confirmCallback();
                }
            }
        }

        private class FoodControllerHandler(CoopPlayer coopPlayer, FoodDrinkItemClass foodDrink, float amount, GStruct353<EBodyPart> bodyParts, int animationVariant)
        {
            private readonly CoopPlayer coopPlayer = coopPlayer;
            private readonly FoodDrinkItemClass foodDrink = foodDrink;
            private readonly float amount = amount;
            private readonly GStruct353<EBodyPart> bodyParts = bodyParts;
            private readonly int animationVariant = animationVariant;
            public Process<MedsController, GInterface176> process;
            public Action confirmCallback;

            internal MedsController ReturnController()
            {
                return MedsController.smethod_6<MedsController>(coopPlayer, foodDrink, bodyParts, amount, animationVariant);
            }

            internal void SendPacket()
            {
                CommonPlayerPacket packet = new()
                {
                    NetId = coopPlayer.NetId,
                    Type = ECommonSubPacketType.Proceed,
                    SubPacket = new ProceedPacket()
                    {
                        ProceedType = EProceedType.MedsClass,
                        ItemId = foodDrink.Id,
                        Amount = amount,
                        AnimationVariant = animationVariant,
                        BodyParts = bodyParts
                    }
                };
                coopPlayer.PacketSender.SendPacket(ref packet);
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
                CommonPlayerPacket packet = new()
                {
                    NetId = coopPlayer.NetId,
                    Type = ECommonSubPacketType.Proceed,
                    SubPacket = new ProceedPacket()
                    {
                        ProceedType = EProceedType.Knife,
                        ItemId = knife.Item.Id
                    }
                };
                coopPlayer.PacketSender.SendPacket(ref packet);
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
            public Process<QuickKnifeKickController, GInterface180> process;
            public Action confirmCallback;

            internal QuickKnifeKickController ReturnController()
            {
                return QuickKnifeKickController.smethod_9<QuickKnifeKickController>(coopPlayer, knife);
            }

            internal void SendPacket()
            {
                CommonPlayerPacket packet = new()
                {
                    NetId = coopPlayer.NetId,
                    Type = ECommonSubPacketType.Proceed,
                    SubPacket = new ProceedPacket()
                    {
                        ProceedType = EProceedType.QuickKnifeKick,
                        ItemId = knife.Item.Id
                    }
                };
                coopPlayer.PacketSender.SendPacket(ref packet);
            }

            internal void HandleResult(IResult result)
            {
                if (result.Succeed)
                {
                    confirmCallback();
                }
            }
        }

        private class GrenadeControllerHandler(CoopPlayer coopPlayer, ThrowWeapItemClass throwWeap)
        {
            private readonly CoopPlayer coopPlayer = coopPlayer;
            private readonly ThrowWeapItemClass throwWeap = throwWeap;
            public Process<GrenadeHandsController, IHandsThrowController> process;
            public Action confirmCallback;

            internal CoopClientGrenadeController ReturnController()
            {
                return CoopClientGrenadeController.Create(coopPlayer, throwWeap);
            }

            internal void SendPacket()
            {
                CommonPlayerPacket packet = new()
                {
                    NetId = coopPlayer.NetId,
                    Type = ECommonSubPacketType.Proceed,
                    SubPacket = new ProceedPacket()
                    {
                        ProceedType = EProceedType.GrenadeClass,
                        ItemId = throwWeap.Id
                    }
                };
                coopPlayer.PacketSender.SendPacket(ref packet);
            }

            internal void HandleResult(IResult result)
            {
                if (result.Succeed)
                {
                    confirmCallback();
                }
            }
        }

        private class QuickGrenadeControllerHandler(CoopPlayer coopPlayer, ThrowWeapItemClass throwWeap)
        {
            private readonly CoopPlayer coopPlayer = coopPlayer;
            private readonly ThrowWeapItemClass throwWeap = throwWeap;
            public Process<QuickGrenadeThrowHandsController, GInterface179> process;
            public Action confirmCallback;

            internal CoopClientQuickGrenadeController ReturnController()
            {
                return CoopClientQuickGrenadeController.Create(coopPlayer, throwWeap);
            }

            internal void SendPacket()
            {
                CommonPlayerPacket packet = new()
                {
                    NetId = coopPlayer.NetId,
                    Type = ECommonSubPacketType.Proceed,
                    SubPacket = new ProceedPacket()
                    {
                        ProceedType = EProceedType.QuickGrenadeThrow,
                        ItemId = throwWeap.Id
                    }
                };
                coopPlayer.PacketSender.SendPacket(ref packet);
            }

            internal void HandleResult(IResult result)
            {
                if (result.Succeed)
                {
                    confirmCallback();
                }
            }
        }

        private class DropHandler(CoopPlayer coopPlayer)
        {
            private readonly CoopPlayer coopPlayer = coopPlayer;

            internal void HandleResult()
            {

            }
        }
    }
    #endregion
}
