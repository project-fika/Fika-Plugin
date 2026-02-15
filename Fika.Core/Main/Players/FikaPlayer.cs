// © 2026 Lacyway All Rights Reserved

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.Communications;
using EFT.HealthSystem;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.SynchronizableObjects;
using EFT.Vehicle;
using Fika.Core.Main.ClientClasses;
using Fika.Core.Main.ClientClasses.HandsControllers;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.HostClasses;
using Fika.Core.Main.ObservedClasses.Snapshotting;
using Fika.Core.Main.PacketHandlers;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.Http;
using Fika.Core.Networking.Models;
using Fika.Core.Networking.Packets.Communication;
using Fika.Core.Networking.Packets.FirearmController.SubPackets;
using Fika.Core.Networking.Packets.Generic;
using Fika.Core.Networking.Packets.Generic.SubPackets;
using Fika.Core.Networking.Packets.Player;
using Fika.Core.Networking.Packets.Player.Common;
using Fika.Core.Networking.Packets.Player.Common.SubPackets;
using Fika.Core.Networking.Packets.World;
using Fika.Core.Networking.VOIP;
using HarmonyLib;
using JsonType;
using static Fika.Core.Main.ClientClasses.ClientInventoryController;

namespace Fika.Core.Main.Players;

/// <summary>
/// <see cref="FikaPlayer"/> is the <see cref="LocalPlayer"/>, there can only be one <see cref="FikaPlayer"/> in every game and that is always yourself.
/// </summary>
public class FikaPlayer : LocalPlayer
{
    #region Fields and Properties
    public IPacketSender PacketSender;
    public float ObservedOverlap;
    public CorpseSyncPackets CorpseSyncPacket;
    public int NetId;
    public bool IsObservedAI;
    public Dictionary<uint, Action<ServerOperationStatus>> OperationCallbacks = [];
    public Snapshotter Snapshotter;
    public CommonPlayerPacket CommonPacket;
    public virtual bool LeftStanceDisabled { get; internal set; }
    public DateTime TalkDateTime { get; internal set; }
    public bool WaitingForCallback
    {
        get
        {
            return OperationCallbacks.Count > 0;
        }
    }
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

    protected MongoID? _lastWeaponId;
    protected Action[] _armorUnsubcribes = new Action[Inventory.ArmorSlots.Length];

    private bool _hasSkilledScav;
    private bool _shouldSendSideEffect;
    private VoipSettingsClass _voipHandler;
    private FikaVOIPController _voipController;
    #endregion

    public static async Task<FikaPlayer> Create(GameWorld gameWorld, int playerId, Vector3 position,
        Quaternion rotation, string layerName, string prefix, EPointOfView pointOfView, Profile profile,
        bool aiControl, EUpdateQueue updateQueue, EUpdateMode armsUpdateMode, EUpdateMode bodyUpdateMode,
        CharacterControllerSpawner.Mode characterControllerMode, Func<float> getSensitivity,
        Func<float> getAimingSensitivity, IStatisticsManager statisticsManager, IViewFilter filter, ISession session,
        int netId)
    {
        var useSimpleAnimator = profile.Info.Settings.UseSimpleAnimator;
        var resourceKey = useSimpleAnimator ? ResourceKeyManagerAbstractClass.ZOMBIE_BUNDLE_NAME : ResourceKeyManagerAbstractClass.PLAYER_BUNDLE_NAME;
        var player = Create<FikaPlayer>(gameWorld, resourceKey, playerId, position, updateQueue, armsUpdateMode,
                    bodyUpdateMode, characterControllerMode, getSensitivity, getAimingSensitivity, prefix, false, useSimpleAnimator);

        player.IsYourPlayer = true;
        player.NetId = netId;
        player._voipHandler = VoipSettingsClass.Default;
        player.CommonPacket = new()
        {
            NetId = netId
        };

        PlayerOwnerInventoryController inventoryController = FikaBackendUtils.IsServer ? new FikaHostInventoryController(player, profile, false)
            : new ClientInventoryController(player, profile, false);

        LocalQuestControllerClass questController;
        if (FikaPlugin.Instance.SharedQuestProgression)
        {
            questController = new ClientSharedQuestController(profile, inventoryController, inventoryController.PlayerSearchController, session, player);
        }
        else
        {
            questController = new ClientQuestController(profile, inventoryController, inventoryController.PlayerSearchController, session, player);
        }
        questController.Init();
        LocalPlayerAchievementControllerClass achievementsController = new(profile, inventoryController, questController.Quests, session, true);
        achievementsController.Init();
        achievementsController.AchievementUnlocked += player.UnlockAchievement;
        achievementsController.Run();
        questController.Run();
        ClientPlayerPrestigeControllerClass prestigeController = new(profile, inventoryController, questController.Quests, session);
        GClass3619 dialogController = new(profile, questController, inventoryController);

        if (FikaBackendUtils.IsServer)
        {
            player.PacketSender = await ServerPacketSender.Create(player);
        }
        else if (FikaBackendUtils.IsClient)
        {
            player.PacketSender = await ClientPacketSender.Create(player);
        }

        var voipState = (!FikaBackendUtils.IsHeadless && Singleton<IFikaNetworkManager>.Instance.AllowVOIP && SoundSettingsControllerClass.CheckMicrophone())
            ? EVoipState.Available : EVoipState.NotAvailable;

        await player.Init(rotation, layerName, pointOfView, profile, inventoryController,
            new ClientHealthController(profile.Health, player, inventoryController, profile.Skills, aiControl),
            statisticsManager, questController, achievementsController, prestigeController, dialogController, filter,
            voipState, false, false);

        foreach (var magazineClass in player.Inventory.GetPlayerItems(EPlayerItems.NonQuestItems).OfType<MagazineItemClass>())
        {
            player.InventoryController.StrictCheckMagazine(magazineClass, true, player.Profile.MagDrillsMastering, false, false);
        }

        var services = Traverse.Create(player).Field<HashSet<ETraderServiceType>>("hashSet_0").Value;
        foreach (var etraderServiceType in Singleton<BackendConfigSettingsClass>.Instance.ServicesData.Keys)
        {
            services.Add(etraderServiceType);
        }

        player._handsController = EmptyHandsController.smethod_6<EmptyHandsController>(player);
        player._handsController.Spawn(1f, FikaGlobals.EmptyAction);
        player.AIData = new PlayerAIDataClass(null, player);
        player.AggressorFound = false;
        player._animators[0].enabled = true;

        var radioTransmitterRecodableComponent = player.FindRadioTransmitter();
        if (radioTransmitterRecodableComponent != null)
        {
            //Todo: (Archangel) method_131 refers to 'singlePlayerInventoryController_0' which is null in our case
            //radioTransmitterRecodableComponent.OnRadioTransmitterStatusChanged += player.method_131;

            if (player.Profile.GetTraderStanding("638f541a29ffd1183d187f57").IsZero())
            {
                radioTransmitterRecodableComponent.SetEncoded(false);
            }
        }

        player.SubscribeToArmorChangeEvent();
        player.RecalculateEquippedArmorComponents(null);

        player.Profile.Info.SetProfileNickname(FikaBackendUtils.PMCName ?? profile.Nickname);

        return player;
    }

    protected void SubscribeToArmorChangeEvent()
    {
        for (var i = 0; i < Inventory.ArmorSlots.Length; i++)
        {
            var slotType = Inventory.ArmorSlots[i];
            var slot = Inventory.Equipment.GetSlot(slotType);

            _armorUnsubcribes[i] = slot.ReactiveContainedItem
                .Subscribe(RecalculateEquippedArmorComponents); // we use subscribe to avoid calling the event on each subscription
        }
    }

    /// <summary>
    /// Recalculates all equipped <see cref="ArmorComponent"/>s when an armor slot changes
    /// </summary>
    /// <param name="item">The item changed</param>
    protected void RecalculateEquippedArmorComponents(Item item)
    {
        _preAllocatedArmorComponents.Clear();
        Inventory.GetPutOnArmorsNonAlloc(_preAllocatedArmorComponents);
    }

    public void AbuseNotification(string reporterId)
    {
        if (IsYourPlayer)
        {
            _voipController?.ReceiveAbuseNotification(reporterId);
        }
    }

    private void UnlockAchievement(string tpl)
    {
        _achievementsController.UnlockAchievementForced(tpl);
    }

    public override void InitVoip(EVoipState voipState)
    {
        if (_voipHandler.VoipEnabled && voipState != EVoipState.NotAvailable)
        {
            var settings = Singleton<SharedGameSettingsClass>.Instance.Sound.Settings;
            if (!settings.VoipEnabled)
            {
                voipState = EVoipState.Off;
            }
            if (!_voipHandler.MicrophoneChecked)
            {
                voipState = EVoipState.MicrophoneFail;
            }
            base.InitVoip(voipState);
            _voipController = new(this, settings);
            VoipController = _voipController;
        }
    }

    public override void CreateNestedSource()
    {
        base.CreateNestedSource();
        NestedStepSoundSource.SetBaseVolume(0.9f);
    }

    public override BasePhysicalClass CreatePhysical()
    {
        return new FikaClientPhysical();
    }

    public override void CreateMovementContext()
    {
        var movement_MASK = EFTHardSettings.Instance.MOVEMENT_MASK;
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
        NotificationManagerClass.DisplayNotification(new GClass2549(skill));
    }

    public override void SendVoiceMuffledState(bool isMuffled)
    {
        Singleton<IFikaNetworkManager>.Instance.SendGenericPacket(EGenericSubPacketType.MuffledState,
                MuffledState.FromValue(NetId, isMuffled), true);
    }

    public override void OnWeaponMastered(MasterSkillClass masterSkill)
    {
        NotificationManagerClass.DisplayMessageNotification(string.Format("MasteringLevelUpMessage".Localized(null),
            masterSkill.MasteringGroup.Id.Localized(null),
            masterSkill.Level.ToString()), ENotificationDurationType.Default, ENotificationIconType.Default, null);
    }

    public override void ApplyDamageInfo(DamageInfoStruct damageInfo, EBodyPart bodyPartType, EBodyPartColliderType colliderType, float absorbed)
    {
        if (IsYourPlayer && damageInfo.Player != null && !FikaPlugin.Instance.FriendlyFire && damageInfo.Player.iPlayer.GroupId == GroupId)
        {
            return;
        }

        if (damageInfo.Weapon != null)
        {
            _lastWeaponId = damageInfo.Weapon.Id;
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
                _lastWeaponId = damageInfo.Weapon.Id;
            }
            return SimulatedApplyShot(damageInfo, bodyPartType, colliderType, armorPlateCollider);
        }

        return null;
    }

    private ShotInfoClass SimulatedApplyShot(DamageInfoStruct damageInfo, EBodyPart bodyPartType, EBodyPartColliderType colliderType, EArmorPlateCollider armorPlateCollider)
    {
        var activeHealthController = ActiveHealthController;
        if (activeHealthController != null && !activeHealthController.IsAlive)
        {
            return null;
        }
        var flag = damageInfo.DeflectedBy != null;
        var damage = damageInfo.Damage;
        var list = ProceedDamageThroughArmor(ref damageInfo, colliderType, armorPlateCollider, true);
        method_97(list);
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
        ApplyDamageInfo(damageInfo, bodyPartType, colliderType, 0f);
        ShotReactions(damageInfo, bodyPartType);
        ReceiveDamage(damageInfo.Damage, bodyPartType, damageInfo.DamageType, num, hitInfo.Material);

        return hitInfo;
    }

    #region Proceed
    public override void Proceed(bool withNetwork, Callback<GInterface198> callback, bool scheduled = true)
    {
        base.Proceed(withNetwork, callback, scheduled);
        CommonPacket.Type = ECommonSubPacketType.Proceed;
        CommonPacket.SubPacket = ProceedPacket.FromValue(default, default, 0f, 0, EProceedType.EmptyHands, scheduled);
        PacketSender.NetworkManager.SendNetReusable(ref CommonPacket, DeliveryMethod.ReliableOrdered, true);
    }

    public override void Proceed(FoodDrinkItemClass foodDrink, float amount, Callback<GInterface203> callback, int animationVariant, bool scheduled = true)
    {
        GStruct382<EBodyPart> bodyparts = new(EBodyPart.Head);
        FoodControllerHandler handler = new(this, foodDrink, amount, bodyparts, animationVariant);

        Func<MedsController> func = new(handler.ReturnController);
        handler.Process = new(this, func, foodDrink, false);
        handler.ConfirmCallback = new(handler.SendPacket);
        handler.Process.method_0(new(handler.HandleResult), callback, scheduled);
    }

    public override void Proceed(MedsItemClass meds, GStruct382<EBodyPart> bodyParts, Callback<GInterface203> callback, int animationVariant, bool scheduled = true)
    {
        MedsControllerHandler handler = new(this, meds, bodyParts, animationVariant);

        Func<MedsController> func = new(handler.ReturnController);
        handler.Process = new(this, func, meds, false);
        handler.ConfirmCallback = new(handler.SendPacket);
        handler.Process.method_0(new(handler.HandleResult), callback, scheduled);
    }

    public override void Proceed<T>(Item item, Callback<GInterface202> callback, bool scheduled = true)
    {
        if (item is PortableRangeFinderItemClass)
        {
            PortableRangeFinderControllerHandler rangeFinderHandler = new(this, item);

            Func<PortableRangeFinderController> rangeFinderFunc = new(rangeFinderHandler.ReturnController);
            rangeFinderHandler.Process = new(this, rangeFinderFunc, item, false);
            rangeFinderHandler.ConfirmCallback = new(rangeFinderHandler.SendPacket);
            rangeFinderHandler.Process.method_0(new(rangeFinderHandler.HandleResult), callback, scheduled);
            return;
        }

        UsableItemControllerHandler handler = new(this, item);

        Func<UsableItemController> func = new(handler.ReturnController);
        handler.Process = new(this, func, item, false);
        handler.ConfirmCallback = new(handler.SendPacket);
        handler.Process.method_0(new(handler.HandleResult), callback, scheduled);
    }

    public override void Proceed(Item item, Callback<IOnHandsUseCallback> callback, bool scheduled = true)
    {
        QuickUseItemControllerHandler handler = new(this, item);

        Func<QuickUseItemController> func = new(handler.ReturnController);
        handler.Process = new(this, func, item, true);
        handler.ConfirmCallback = new(handler.SendPacket);
        handler.Process.method_0(new(handler.HandleResult), callback, scheduled);
    }

    public override void Proceed(KnifeComponent knife, Callback<IKnifeController> callback, bool scheduled = true)
    {
        KnifeControllerHandler handler = new(this, knife);

        Func<KnifeController> func = new(handler.ReturnController);
        handler.Process = new(this, func, handler.Knife.Item, false);
        handler.ConfirmCallback = new(handler.SendPacket);
        handler.Process.method_0(new(handler.HandleResult), callback, scheduled);
    }

    public override void Proceed(KnifeComponent knife, Callback<GInterface207> callback, bool scheduled = true)
    {
        QuickKnifeControllerHandler handler = new(this, knife);

        Func<QuickKnifeKickController> func = new(handler.ReturnController);
        handler.Process = new(this, func, handler.Knife.Item, true);
        handler.ConfirmCallback = new(handler.SendPacket);
        handler.Process.method_0(new(handler.HandleResult), callback, scheduled);
    }

    public override void Proceed(ThrowWeapItemClass throwWeap, Callback<GInterface206> callback, bool scheduled = true)
    {
        QuickGrenadeControllerHandler handler = new(this, throwWeap);

        Func<QuickGrenadeThrowHandsController> func = new(handler.ReturnController);
        handler.Process = new(this, func, throwWeap, false);
        handler.ConfirmCallback = new(handler.SendPacket);
        handler.Process.method_0(new(handler.HandleResult), callback, scheduled);
    }

    public override void Proceed(ThrowWeapItemClass throwWeap, Callback<IHandsThrowController> callback, bool scheduled = true)
    {
        GrenadeControllerHandler handler = new(this, throwWeap);

        Func<GrenadeHandsController> func = new(handler.ReturnController);
        handler.Process = new(this, func, throwWeap, false);
        handler.ConfirmCallback = new(handler.SendPacket);
        handler.Process.method_0(new(handler.HandleResult), callback, scheduled);
    }

    public override void Proceed(Weapon weapon, Callback<IFirearmHandsController> callback, bool scheduled = true)
    {
        FirearmControllerHandler handler = new(this, weapon);
        var flag = false;
        if (_handsController is FirearmController firearmController)
        {
            flag = firearmController.CheckForFastWeaponSwitch(handler.Weapon);
        }
        Func<FirearmController> func = new(handler.ReturnController);
        handler.Process = new Process<FirearmController, IFirearmHandsController>(this, func, handler.Weapon, flag);
        handler.ConfirmCallback = new(handler.SendPacket);
        handler.Process.method_0(new(handler.HandleResult), callback, scheduled);
    }
    #endregion

    public override void DropCurrentController(Action callback, bool fastDrop, Item nextControllerItem = null)
    {
        CommonPacket.Type = ECommonSubPacketType.Drop;
        CommonPacket.SubPacket = DropPacket.FromValue(fastDrop);
        PacketSender.NetworkManager.SendNetReusable(ref CommonPacket, DeliveryMethod.ReliableOrdered, true);
        base.DropCurrentController(callback, fastDrop, nextControllerItem);
    }

    public override void OnBeenKilledByAggressor(IPlayer aggressor, DamageInfoStruct damageInfo, EBodyPart bodyPart, EDamageType lethalDamageType)
    {
        base.OnBeenKilledByAggressor(aggressor, damageInfo, bodyPart, lethalDamageType);

        // Handle 'Help Scav' rep gains
        if (aggressor is FikaPlayer fikaPlayer)
        {
            if (fikaPlayer.Side == EPlayerSide.Savage)
            {
                fikaPlayer.Loyalty.method_1(this);
            }

            if (Side == EPlayerSide.Savage && fikaPlayer.Side != EPlayerSide.Savage && !fikaPlayer._hasSkilledScav)
            {
                fikaPlayer._hasSkilledScav = true;
                return;
            }
            else if (Side != EPlayerSide.Savage && _hasSkilledScav && aggressor.Side == EPlayerSide.Savage)
            {
                fikaPlayer.Profile?.FenceInfo?.AddStanding(Profile.Info.Settings.StandingForKill, EFT.Counters.EFenceStandingSource.ScavHelp);
            }
        }
    }

    public override void MouseLook(bool forceApplyToOriginalRibcage = false)
    {
        MovementContext.RotationAction?.Invoke(this);
    }

    protected Item FindWeapon()
    {
#if DEBUG
        FikaGlobals.LogWarning($"Finding weapon '{_lastWeaponId}'!");
#endif
        var itemResult = FindItemById(_lastWeaponId.Value, false, false);
        var item = itemResult.Value;
        if (!itemResult.Succeeded)
        {
            for (var i = 0; i < Singleton<IFikaGame>.Instance.GameController.ThrownGrenades.Count; i++)
            {
                var grenadeClass = Singleton<IFikaGame>.Instance.GameController.ThrownGrenades[i];
                if (grenadeClass.Id == _lastWeaponId)
                {
                    item = grenadeClass;
                    break;
                }
            }
        }

        if (item == null)
        {
            var stationaryWeapon = GameWorld.FindStationaryWeaponByItemId(_lastWeaponId);
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

        var item = FindWeapon();
        if (item == null)
        {
            FikaGlobals.LogError($"Could not find killer weapon: {_lastWeaponId}!");
            return;
        }
        LastDamageInfo.Weapon = item;
    }

    public void HandleTeammateKill(DamageInfoStruct damage, EBodyPart bodyPart,
        EPlayerSide playerSide, WildSpawnType role, string playerProfileId,
        float distance, List<string> targetEquipment,
        HealthEffects enemyEffects, List<string> zoneIds, FikaPlayer killer)
    {
        if (!HealthController.IsAlive)
        {
            return;
        }

#if DEBUG
        FikaGlobals.LogWarning($"HandleTeammateKill: Weapon {(damage.Weapon != null ? damage.Weapon.Name.Localized() : "None")}");
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

        for (var i = 0; i < list.Count; i++)
        {
            var value = list[i];
            AbstractQuestControllerClass.CheckKillConditionCounter(value, playerProfileId, targetEquipment, damage.Weapon,
                            bodyPart, Location, distance, role.ToStringNoBox(), CurrentHour, enemyEffects,
                            killer.HealthController.BodyPartEffects, zoneIds, killer.HealthController.ActiveBuffsNames());
        }
    }

    protected void HandleSharedExperience(bool countAsBoss, int experience, SessionCountersClass sessionCounters)
    {
        if (experience <= 0)
        {
            experience = Singleton<BackendConfigSettingsClass>.Instance.Experience.Kill.VictimBotLevelExp;
        }

        if (FikaPlugin.Instance.Settings.SharedKillExperience.Value && !countAsBoss)
        {
            var toReceive = experience / 2;
#if DEBUG
            FikaGlobals.LogInfo($"Received shared kill XP of {toReceive}");
#endif
            sessionCounters.AddLong(1L, SessionCounterTypesAbstractClass.Kills);
            sessionCounters.AddInt(toReceive, SessionCounterTypesAbstractClass.ExpKillBase);
        }

        if (FikaPlugin.Instance.Settings.SharedBossExperience.Value && countAsBoss)
        {
            var toReceive = experience / 2;
#if DEBUG
            FikaGlobals.LogInfo($"Received shared boss XP of {toReceive}");
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
            EFT.UI.ConsoleScreen.Log(message);
            FikaGlobals.LogInfo(message);
        }
    }
#endif

    public override void SetInventoryOpened(bool opened)
    {
        if (this is ObservedPlayer)
        {
            base.SetInventoryOpened(opened);
            return;
        }

        base.SetInventoryOpened(opened);

        CommonPacket.Type = ECommonSubPacketType.InventoryChanged;
        CommonPacket.SubPacket = InventoryChangedPacket.FromValue(opened);
        PacketSender.NetworkManager.SendNetReusable(ref CommonPacket, DeliveryMethod.ReliableOrdered, true);
    }

    public override void SendHeadlightsPacket(bool isSilent)
    {
        if (PacketSender != null && PacketSender.NetworkManager != null)
        {
            FirearmLightStateStruct[] lightStates = [.. _helmetLightControllers.Select(FikaGlobals.GetFirearmLightStates)];

            CommonPacket.Type = ECommonSubPacketType.HeadLights;
            CommonPacket.SubPacket = HeadLightsPacket.FromValue(lightStates.Length, isSilent, lightStates);
            PacketSender.NetworkManager.SendNetReusable(ref CommonPacket, DeliveryMethod.ReliableOrdered, true);
        }
    }

    public override void SendWeaponLightPacket()
    {
        if (PacketSender == null)
        {
            return;
        }

        if (HandsController is FikaClientFirearmController controller)
        {
            FirearmLightStateStruct[] array = [.. controller.Item.AllSlots
                .Select(FikaGlobals.GetContainedItem)
                .GetComponents<LightComponent>()
                .Select(FikaGlobals.GetFirearmLightStatesFromComponent)];

            if (array.Length == 0)
            {
                return;
            }

            controller.SendLightStates(LightStatesPacket.FromValue(array.Length, array));
        }
    }

    public override void OnPhraseTold(EPhraseTrigger @event, TaggedClip clip, TagBank bank, PhraseSpeakerClass speaker)
    {
        base.OnPhraseTold(@event, clip, bank, speaker);

        if (ActiveHealthController.IsAlive)
        {
            CommonPacket.Type = ECommonSubPacketType.Phrase;
            CommonPacket.SubPacket = PhrasePacket.FromValue(@event, clip.NetId);
            PacketSender.NetworkManager.SendNetReusable(ref CommonPacket, DeliveryMethod.ReliableOrdered, true);
        }
    }

    public override Corpse CreateCorpse()
    {
        if (FikaBackendUtils.IsServer)
        {
            var corpse = base.CreateCorpse();
            corpse.IsZombieCorpse = UsedSimplifiedSkeleton;
            //CorpsePositionSyncer.Create(corpse.gameObject, corpse, NetId);
            return corpse;
        }

        var observedCorpse = CreateCorpse<ObservedCorpse>(Velocity);
        observedCorpse.IsZombieCorpse = UsedSimplifiedSkeleton;
        observedCorpse.SetSpecificSettings(PlayerBones.RightPalm);
        Singleton<GameWorld>.Instance.ObservedPlayersCorpses.Add(NetId, observedCorpse);
        return observedCorpse;
    }

    public override void OperateStationaryWeapon(StationaryWeapon stationaryWeapon, StationaryPacketStruct.EStationaryCommand command)
    {
        if (command is StationaryPacketStruct.EStationaryCommand.Occupy)
        {
            if (WaitingForCallback || !HandsController.CanRemove())
            {
                return;
            }
        }

        base.OperateStationaryWeapon(stationaryWeapon, command);

        CommonPacket.Type = ECommonSubPacketType.Stationary;
        CommonPacket.SubPacket = StationaryPacket.FromValue((EStationaryCommand)command, stationaryWeapon.Id);
        PacketSender.NetworkManager.SendNetReusable(ref CommonPacket, DeliveryMethod.ReliableOrdered, true);
    }

    // Start
    public override void vmethod_0(WorldInteractiveObject interactiveObject, InteractionResult interactionResult, Action callback)
    {
        CommonPacket.Type = ECommonSubPacketType.WorldInteraction;
        CommonPacket.SubPacket = WorldInteractionPacket.FromValue(interactiveObject.Id, interactionResult.InteractionType,
            EInteractionStage.Start, (interactionResult is KeyInteractionResultClass keyInteractionResult) ? keyInteractionResult.Key.Item.Id : null);
        PacketSender.NetworkManager.SendNetReusable(ref CommonPacket, DeliveryMethod.ReliableOrdered, true);
        CurrentManagedState.StartDoorInteraction(interactiveObject, interactionResult, callback);
        UpdateInteractionCast();
    }

    // Execute
    public override void vmethod_1(WorldInteractiveObject door, InteractionResult interactionResult)
    {
        base.vmethod_1(door, interactionResult);
        if (!door.ForceLocalInteraction)
        {
            CommonPacket.Type = ECommonSubPacketType.WorldInteraction;
            CommonPacket.SubPacket = WorldInteractionPacket.FromValue(door.Id, interactionResult.InteractionType,
                EInteractionStage.Execute, (interactionResult is KeyInteractionResultClass keyInteractionResult) ? keyInteractionResult.Key.Item.Id : null);
            PacketSender.NetworkManager.SendNetReusable(ref CommonPacket, DeliveryMethod.ReliableOrdered, true);
        }
        UpdateInteractionCast();
    }

    public override void vmethod_6(string itemId, string zoneId, bool successful)
    {
        base.vmethod_6(itemId, zoneId, successful);
        UpdateInteractionCast();
    }

    public override void OnAnimatedInteraction(EInteraction interaction)
    {
        if (!FikaGlobals.BlockedInteractions.Contains(interaction))
        {
            CommonPacket.Type = ECommonSubPacketType.Interaction;
            CommonPacket.SubPacket = InteractionPacket.FromValue(interaction);
            PacketSender.NetworkManager.SendNetReusable(ref CommonPacket, DeliveryMethod.ReliableOrdered, true);
        }
    }

    public override void HealthControllerUpdate(float deltaTime)
    {
        _healthController?.ManualUpdate(deltaTime);
    }

    public override void UpdateTriggerColliderSearcher(float deltaTime, bool isCloseToCamera = true)
    {
        _triggerColliderSearcher.ManualUpdate(deltaTime);
    }

    public override void OnMounting(MountingPacketStruct.EMountingCommand command)
    {
        var packet = MountingPacket.FromValue(command, MovementContext.IsInMountedState, MovementContext.IsInMountedState ? MovementContext.PlayerMountingPointData.MountPointData.MountDirection : default,
            MovementContext.IsInMountedState ? MovementContext.PlayerMountingPointData.MountPointData.MountPoint : default,
            MovementContext.IsInMountedState ? MovementContext.PlayerMountingPointData.CurrentMountingPointVerticalOffset : 0f,
           MovementContext.IsInMountedState ? (short)MovementContext.PlayerMountingPointData.MountPointData.MountSideDirection : (short)0);

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

        CommonPacket.Type = ECommonSubPacketType.Mounting;
        CommonPacket.SubPacket = packet;
        PacketSender.NetworkManager.SendNetReusable(ref CommonPacket,
            command is MountingPacketStruct.EMountingCommand.Update ? DeliveryMethod.Unreliable : DeliveryMethod.ReliableOrdered,
            true);
    }

    public override void vmethod_3(TransitControllerAbstractClass controller, int transitPointId, string keyId, EDateTime time)
    {
        var packet = controller.GetInteractPacket(transitPointId, keyId, time);
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
            Singleton<FikaClient>.Instance.SendData(ref interactPacket, DeliveryMethod.ReliableOrdered);
            if (Singleton<GameWorld>.Instance.TransitController is ClientTransitController transitController)
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

        Singleton<IFikaNetworkManager>.Instance.SendGenericPacket(EGenericSubPacketType.DisarmTripwire,
                DisarmTripwire.FromValue(data), true);
    }

    public override void vmethod_5(GClass2282 controller, int objectId, EventObject.EInteraction interaction)
    {
        var packet = controller.GetInteractPacket(objectId, interaction);
        if (FikaBackendUtils.IsServer)
        {
            controller.InteractWithEventObject(this, packet);
        }
        else
        {
            EventControllerInteractPacket interactPacket = new()
            {
                NetId = NetId,
                Data = packet
            };
            PacketSender.NetworkManager.SendData(ref interactPacket, DeliveryMethod.ReliableOrdered, true);
        }
        UpdateInteractionCast();
    }

    public override void ApplyCorpseImpulse()
    {
        Corpse.Ragdoll.ApplyImpulse(LastDamageInfo.HitCollider, LastDamageInfo.Direction, LastDamageInfo.HitPoint, _corpseAppliedForce);
    }

    public void SetupCorpseSyncPacket(NetworkHealthSyncPacketStruct packet)
    {
        var num = EFTHardSettings.Instance.HIT_FORCE;
        num *= 0.3f + 0.7f * Mathf.InverseLerp(50f, 20f, LastDamageInfo.PenetrationPower);
        _corpseAppliedForce = num;

        if (FikaBackendUtils.IsServer || IsYourPlayer)
        {
            if (Side is not EPlayerSide.Savage)
            {
                GenerateDogtagDetails();
            }
        }

        var inventoryDescriptor = EFTItemSerializerClass.SerializeItem(Inventory.Equipment, FikaGlobals.SearchControllerSerializer);

        var packets = HealthSyncPacket.FromValue(packet);
        packets.BodyPart = LastBodyPart;
        packets.CorpseSyncPacket = new()
        {
            BodyPartColliderType = LastDamageInfo.BodyPartColliderType,
            Direction = LastDamageInfo.Direction,
            Point = LastDamageInfo.HitPoint,
            Force = _corpseAppliedForce,
            InventoryDescriptor = inventoryDescriptor,
            ItemSlot = EquipmentSlot.ArmBand
        };

        if (TriggerZones.Count > 0)
        {
            packets.TriggerZones.AddRange(TriggerZones);
        }

        if (LastAggressor != null)
        {
            packets.KillerId = LastAggressor.ProfileId;
        }

        if (_lastWeaponId != null)
        {
            packets.WeaponId = _lastWeaponId;
        }

        if (HandsController.Item != null)
        {
            var heldItem = HandsController.Item;
            for (var i = 0; i < FikaGlobals.WeaponSlots.Count; i++)
            {
                var weaponSlot = FikaGlobals.WeaponSlots[i];
                if (heldItem == Equipment.GetSlot(weaponSlot).ContainedItem)
                {
                    packets.CorpseSyncPacket.ItemSlot = weaponSlot;
                    break;
                }
            }
        }

        CommonPacket.Type = ECommonSubPacketType.HealthSync;
        CommonPacket.SubPacket = packets;
        PacketSender.NetworkManager.SendNetReusable(ref CommonPacket, DeliveryMethod.ReliableOrdered, true);
    }

    public override void OnDead(EDamageType damageType)
    {
        foreach (var unsubcribe in _armorUnsubcribes)
        {
            unsubcribe?.Invoke();
        }

        if (LastDamageInfo.Weapon == null && _lastWeaponId != null)
        {
            FindKillerWeapon();
#if DEBUG
            if (LastDamageInfo.Weapon != null)
            {
                FikaGlobals.LogWarning($"Found weapon '{LastDamageInfo.Weapon.Name.Localized()}'!");
            }
#endif
        }
        base.OnDead(damageType);
        PacketSender.SendState = false;
        if (IsYourPlayer)
        {
            StartCoroutine(LocalPlayerDied());
        }
    }

    public override void OnDestroy()
    {
        if (IsAI || IsYourPlayer)
        {
            CommonPacket?.Clear();
            CommonPacket = null;
        }
        base.OnDestroy();
    }

    private void GenerateDogtagDetails()
    {
        if (LastDamageInfo.Weapon == null && _lastWeaponId != null)
        {
            FindKillerWeapon();
#if DEBUG
            if (LastDamageInfo.Weapon != null)
            {
                FikaGlobals.LogWarning($"Found weapon '{LastDamageInfo.Weapon.Name.Localized()}'!");
            }
#endif
        }

        var accountId = AccountId;
        var profileId = ProfileId;
        var nickname = Profile.Nickname;
        var hasAggressor = LastAggressor != null;
        var killerAccountId = hasAggressor ? LastAggressor.AccountId : string.Empty;
        var killerProfileId = hasAggressor ? LastAggressor.ProfileId : string.Empty;
        var killerNickname = (hasAggressor && !string.IsNullOrEmpty(LastAggressor.Profile.Nickname)) ? LastAggressor.Profile.Nickname : string.Empty;
        var side = Side;
        var level = Profile.Info.Level;
        var time = EFTDateTimeClass.UtcNow.ToLocalTime();
        var weaponName = LastAggressor != null ? (LastDamageInfo.Weapon != null ? LastDamageInfo.Weapon.ShortName : string.Empty) : "-";
        var groupId = GroupId;

        var item = Equipment.GetSlot(EquipmentSlot.Dogtag).ContainedItem;
        if (item != null)
        {
            var dogtagComponent = item.GetItemComponent<DogtagComponent>();
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

        FikaGlobals.LogError($"GenerateAndSendDogTagPacket: Item or Dogtagcomponent was null on player {Profile.Nickname}, id {NetId}");
    }

    private IEnumerator LocalPlayerDied()
    {
        AddPlayerRequest request = new(FikaBackendUtils.GroupId, ProfileId, FikaBackendUtils.IsSpectator);
        var diedTask = FikaRequestHandler.PlayerDied(request);
        WaitForEndOfFrame waitForEndOfFrame = new();
        while (!diedTask.IsCompleted)
        {
            yield return waitForEndOfFrame;
        }
    }

    public override void TryInteractionCallback(LootableContainer container)
    {
        LootableContainerInteractionHandler handler = new(this, container);
        if (handler.Container != null && _openAction != null)
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

        var client = Singleton<FikaClient>.Instance;
        BTRInteractionPacket packet = new(NetId)
        {
            Data = btr.GetInteractWithBtrPacket(placeId, interaction)
        };
        client.SendData(ref packet, DeliveryMethod.ReliableOrdered);
        UpdateInteractionCast();
    }

    public void SetupMainPlayer()
    {
        Profile.Info.GroupId = "Fika";
        Profile.Info.TeamId = "Fika";
    }

    public void HandleHeadLightsPacket(HeadLightsPacket packet)
    {
        try
        {
            if (_helmetLightControllers != null)
            {
                for (var i = 0; i < _helmetLightControllers.Count(); i++)
                {
                    _helmetLightControllers.ElementAt(i)?.LightMod?.SetLightState(packet.LightStates[i]);
                }
                if (!packet.IsSilent)
                {
                    SwitchHeadLightsAnimation();
                }
            }
        }
        catch
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
                usableItemController.SetAim(packet.AimState);
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
        if (OperationCallbacks.TryGetValue(operationCallbackPacket.CallbackId, out var callback))
        {
            if (operationCallbackPacket.Status != EOperationStatus.Started)
            {
                OperationCallbacks.Remove(operationCallbackPacket.CallbackId);
            }
            ServerOperationStatus status = new(operationCallbackPacket.Status, operationCallbackPacket.Error);
            callback(status);
        }
        else
        {
            FikaGlobals.LogError($"Could not find CallbackId: {operationCallbackPacket.CallbackId}!");
        }
    }

    public override void ApplyExplosionDamageToArmor(Dictionary<ExplosiveHitArmorColliderStruct, float> armorDamage, DamageInfoStruct damageInfo)
    {
        if (IsYourPlayer)
        {
            for (var i = 0; i < _preAllocatedArmorComponents.Count; i++)
            {
                var armorComponent = _preAllocatedArmorComponents[i];
                var num = 0f;
                foreach ((var colliderStruct, var amount) in armorDamage)
                {
                    if (armorComponent.ShotMatches(colliderStruct.BodyPartColliderType, colliderStruct.ArmorPlateCollider))
                    {
                        num += amount;
                    }
                }

                if (num > 0f)
                {
                    num = armorComponent.ApplyExplosionDurabilityDamage(num, damageInfo, _preAllocatedArmorComponents);
                    method_96(num, armorComponent);
                    OnArmorPointsChanged(armorComponent);
                }
            }
        }
    }

    public override void OnArmorPointsChanged(ArmorComponent armor, bool children = false)
    {
        if (!children)
        {
            CommonPacket.Type = ECommonSubPacketType.ArmorDamage;
            CommonPacket.SubPacket = ArmorDamagePacket.FromValue(armor.Item.Id, armor.Repairable.Durability);
            Singleton<IFikaNetworkManager>.Instance.SendNetReusable(ref CommonPacket, DeliveryMethod.ReliableOrdered, true);
        }
    }

    /*public void SendArmorDamagePacket()
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
            PacketSender.NetworkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
        }
    }*/

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
                if (IsYourPlayer && !FikaPlugin.Instance.FriendlyFire && damageInfo.Player.iPlayer.GroupId == GroupId)
                {
                    return;
                }
            }
            _lastWeaponId = packet.WeaponId;
        }

        if (damageInfo.DamageType == EDamageType.Melee && _lastWeaponId != null)
        {
            var item = FindWeapon();
            if (item != null)
            {
                damageInfo.Weapon = item;
                (damageInfo.Player.iPlayer as FikaPlayer)._shouldSendSideEffect = true;
#if DEBUG
                FikaGlobals.LogWarning("Found weapon for knife damage: " + item.Name.Localized());
#endif
            }
        }

        ShotReactions(damageInfo, packet.BodyPartType);
        ReceiveDamage(damageInfo.Damage, packet.BodyPartType, damageInfo.DamageType, packet.Absorbed, packet.Material);
        base.ApplyDamageInfo(damageInfo, packet.BodyPartType, packet.ColliderType, packet.Absorbed);
    }

    public override void OnSideEffectApplied(SideEffectComponent sideEffect)
    {
        if (!_shouldSendSideEffect)
        {
            return;
        }

        SideEffectPacket packet = new()
        {
            ItemId = sideEffect.Item.Id,
            Value = sideEffect.Value
        };

        PacketSender.NetworkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
        _shouldSendSideEffect = false;
    }

    public void HandleArmorDamagePacket(ArmorDamagePacket packet)
    {
        for (var i = 0; i < _preAllocatedArmorComponents.Count; i++)
        {
            var armorComponent = _preAllocatedArmorComponents[i];
            if (armorComponent.Item.Id == packet.ItemId)
            {
                armorComponent.Repairable.Durability = packet.Durability;
                armorComponent.Buff.TryDisableComponent(armorComponent.Repairable.Durability);
                armorComponent.Item.RaiseRefreshEvent(false, false);
                return;
            }
        }

        var gstruct = Singleton<GameWorld>.Instance.FindItemById(packet.ItemId);
        if (gstruct.Failed)
        {
            FikaGlobals.LogError("HandleArmorDamagePacket: " + gstruct.Error);
            return;
        }

        var itemComponent = gstruct.Value.GetItemComponent<ArmorComponent>();
        if (itemComponent != null)
        {
            itemComponent.Repairable.Durability = packet.Durability;
            itemComponent.Buff.TryDisableComponent(itemComponent.Repairable.Durability);
            itemComponent.Item.RaiseRefreshEvent(false, false);
        }
    }

    public override bool SetShotStatus(BodyPartCollider bodypart, EftBulletClass shot, Vector3 hitpoint, Vector3 shotNormal, Vector3 shotDirection)
    {
        var armorPlateCollider = bodypart as ArmorPlateCollider;
        var earmorPlateCollider = ((armorPlateCollider == null) ? 0 : armorPlateCollider.ArmorPlateColliderType);
        for (var i = 0; i < _preAllocatedArmorComponents.Count; i++)
        {
            var armorComponent = _preAllocatedArmorComponents[i];
            if (armorComponent.ShotMatches(bodypart.BodyPartColliderType, earmorPlateCollider))
            {
                if (armorComponent.Deflects(shotDirection, shotNormal, shot))
                {
                    return true;
                }
                if (shot.BlockedBy == null)
                {
                    armorComponent.SetPenetrationStatus(shot);
                }
            }
        }
        return false;
    }

    public override void UpdateTick()
    {
        _voipController?.Update();
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
        foreach (var unsubcribe in _armorUnsubcribes)
        {
            unsubcribe?.Invoke();
        }

        if (PacketSender != null)
        {
            PacketSender.DestroyThis();
            PacketSender = null;
        }

        _voipController?.Dispose();
        _lastWeaponId = null;
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
        CommonPacket.Type = ECommonSubPacketType.Vault;
        CommonPacket.SubPacket = VaultPacket.FromValue(VaultingParameters.GetVaultingStrategy(), VaultingParameters.MaxWeightPointPosition,
            VaultingParameters.VaultingHeight, VaultingParameters.VaultingLength, MovementContext.VaultingSpeed,
            VaultingParameters.BehindObstacleRatio, VaultingParameters.AbsoluteForwardVelocity);
        PacketSender.NetworkManager.SendNetReusable(ref CommonPacket, DeliveryMethod.ReliableOrdered, true);
    }

    public void ReceiveTraderServicesData(List<TraderServicesClass> services)
    {
        if (!IsYourPlayer)
        {
            return;
        }

        var servicesData = Singleton<BackendConfigSettingsClass>.Instance.ServicesData;

        for (var i = 0; i < services.Count; i++)
        {
            var service = services[i];
            BackendConfigSettingsClass.ServiceData serviceData = new(service, null);
            if (servicesData.ContainsKey(serviceData.ServiceType))
            {
                servicesData[serviceData.ServiceType] = serviceData;
            }
            else
            {
                servicesData.Add(serviceData.ServiceType, serviceData);
            }
            if (!Profile.TradersInfo.TryGetValue(serviceData.TraderId, out var traderInfo))
            {
                FikaGlobals.LogWarning($"Can't find trader with id: {serviceData.TraderId}!");
            }
            else
            {
                traderInfo.SetServiceAvailability(serviceData.ServiceType, service.CanAfford, service.WasPurchasedInThisRaid);
            }
        }
    }

    public Item FindQuestItem(MongoID templateId)
    {
        for (var i = 0; i < Singleton<GameWorld>.Instance.LootList.Count; i++)
        {
            var lootItem = Singleton<GameWorld>.Instance.LootList[i];
            if (lootItem is LootItem observedLootItem && observedLootItem.Item.TemplateId == templateId && observedLootItem.isActiveAndEnabled)
            {
                return observedLootItem.Item;
            }
        }
#if DEBUG
        FikaGlobals.LogInfo($"Could not find questItem with id '{templateId}' in the current session, either the quest is not active or something else occured.");
#endif
        return null;
    }

    #region handlers
    public class KeyHandler(FikaPlayer player)
    {
        public GStruct156<KeyInteractionResultClass> UnlockResult;
        private readonly FikaPlayer _player = player;

        internal void HandleKeyEvent()
        {
            UnlockResult.Value.RaiseEvents(_player._inventoryController, CommandStatus.Succeed);
        }
    }

    private class LootableContainerInteractionHandler(FikaPlayer player, LootableContainer container)
    {
        private readonly FikaPlayer _player = player;
        public readonly LootableContainer Container = container;

        public void Handle()
        {
            _player.CommonPacket.Type = ECommonSubPacketType.ContainerInteraction;
            _player.CommonPacket.SubPacket = ContainerInteractionPacket.FromValue(Container.Id, EInteractionType.Close);
            _player.PacketSender.NetworkManager.SendNetReusable(ref _player.CommonPacket, DeliveryMethod.ReliableOrdered, true);

            Container.Interact(new InteractionResult(EInteractionType.Close));
            if (_player.MovementContext.LevelOnApproachStart > 0f)
            {
                _player.MovementContext.SetPoseLevel(_player.MovementContext.LevelOnApproachStart, false);
                _player.MovementContext.LevelOnApproachStart = -1f;
            }
        }
    }

    private class FirearmControllerHandler(FikaPlayer fikaPlayer, Weapon weapon)
    {
        private readonly FikaPlayer _fikaPlayer = fikaPlayer;
        public readonly Weapon Weapon = weapon;
        public Process<FirearmController, IFirearmHandsController> Process;
        public Action ConfirmCallback;

        internal FikaClientFirearmController ReturnController()
        {
            return FikaClientFirearmController.Create(_fikaPlayer, Weapon);
        }

        internal void SendPacket()
        {
            _fikaPlayer.CommonPacket.Type = ECommonSubPacketType.Proceed;
            _fikaPlayer.CommonPacket.SubPacket = ProceedPacket.FromValue(default, Weapon.Id, 0f, 0,
                Weapon.IsStationaryWeapon ? EProceedType.Stationary : EProceedType.Weapon, false);
            _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _fikaPlayer.CommonPacket, DeliveryMethod.ReliableOrdered, true);
        }

        internal void HandleResult(IResult result)
        {
            if (result.Succeed)
            {
                ConfirmCallback();
            }
        }
    }

    private class UsableItemControllerHandler(FikaPlayer fikaPlayer, Item item)
    {
        private readonly FikaPlayer _fikaPlayer = fikaPlayer;
        private readonly Item _item = item;
        public Process<UsableItemController, GInterface202> Process;
        public Action ConfirmCallback;

        internal FikaClientUsableItemController ReturnController()
        {
            return FikaClientUsableItemController.Create(_fikaPlayer, _item);
        }

        internal void SendPacket()
        {
            _fikaPlayer.CommonPacket.Type = ECommonSubPacketType.Proceed;
            _fikaPlayer.CommonPacket.SubPacket = ProceedPacket.FromValue(default, _item.Id, 0f, 0, EProceedType.UsableItem, false);
            _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _fikaPlayer.CommonPacket, DeliveryMethod.ReliableOrdered, true);
        }

        internal void HandleResult(IResult result)
        {
            if (result.Succeed)
            {
                ConfirmCallback();
            }
        }
    }

    private class PortableRangeFinderControllerHandler(FikaPlayer fikaPlayer, Item item)
    {
        private readonly FikaPlayer _fikaPlayer = fikaPlayer;
        private readonly Item _item = item;
        public Process<PortableRangeFinderController, GInterface202> Process;
        public Action ConfirmCallback;

        internal FikaClientPortableRangeFinderController ReturnController()
        {
            return FikaClientPortableRangeFinderController.Create(_fikaPlayer, _item);
        }

        internal void SendPacket()
        {
            _fikaPlayer.CommonPacket.Type = ECommonSubPacketType.Proceed;
            _fikaPlayer.CommonPacket.SubPacket = ProceedPacket.FromValue(default, _item.Id, 0f, 0, EProceedType.UsableItem, false);
            _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _fikaPlayer.CommonPacket, DeliveryMethod.ReliableOrdered, true);
        }

        internal void HandleResult(IResult result)
        {
            if (result.Succeed)
            {
                ConfirmCallback();
            }
        }
    }

    private class QuickUseItemControllerHandler(FikaPlayer fikaPlayer, Item item)
    {
        private readonly FikaPlayer _fikaPlayer = fikaPlayer;
        private readonly Item _item = item;
        public Process<QuickUseItemController, IOnHandsUseCallback> Process;
        public Action ConfirmCallback;

        internal QuickUseItemController ReturnController()
        {
            return QuickUseItemController.smethod_6<QuickUseItemController>(_fikaPlayer, _item);
        }

        internal void SendPacket()
        {
            _fikaPlayer.CommonPacket.Type = ECommonSubPacketType.Proceed;
            _fikaPlayer.CommonPacket.SubPacket = ProceedPacket.FromValue(default, _item.Id, 0f, 0, EProceedType.QuickUse, false);
            _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _fikaPlayer.CommonPacket, DeliveryMethod.ReliableOrdered, true);
        }

        internal void HandleResult(IResult result)
        {
            if (result.Succeed)
            {
                ConfirmCallback();
            }
        }
    }

    private class MedsControllerHandler(FikaPlayer fikaPlayer, MedsItemClass meds, GStruct382<EBodyPart> bodyParts, int animationVariant)
    {
        private readonly FikaPlayer _fikaPlayer = fikaPlayer;
        private readonly MedsItemClass _meds = meds;
        private readonly GStruct382<EBodyPart> _bodyParts = bodyParts;
        private readonly int _animationVariant = animationVariant;
        public Process<MedsController, GInterface203> Process;
        public Action ConfirmCallback;

        internal MedsController ReturnController()
        {
            return MedsController.smethod_6<MedsController>(_fikaPlayer, _meds, _bodyParts, 1f, _animationVariant);
        }

        internal void SendPacket()
        {
            _fikaPlayer.CommonPacket.Type = ECommonSubPacketType.Proceed;
            _fikaPlayer.CommonPacket.SubPacket = ProceedPacket.FromValue(_bodyParts, _meds.Id, 1f, _animationVariant,
                EProceedType.MedsClass, false);
            _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _fikaPlayer.CommonPacket, DeliveryMethod.ReliableOrdered, true);
        }

        internal void HandleResult(IResult result)
        {
            if (result.Succeed)
            {
                ConfirmCallback();
            }
        }
    }

    private class FoodControllerHandler(FikaPlayer fikaPlayer, FoodDrinkItemClass foodDrink, float amount, GStruct382<EBodyPart> bodyParts, int animationVariant)
    {
        private readonly FikaPlayer _fikaPlayer = fikaPlayer;
        private readonly FoodDrinkItemClass _foodDrink = foodDrink;
        private readonly float _amount = amount;
        private readonly GStruct382<EBodyPart> _bodyParts = bodyParts;
        private readonly int _animationVariant = animationVariant;
        public Process<MedsController, GInterface203> Process;
        public Action ConfirmCallback;

        internal MedsController ReturnController()
        {
            return MedsController.smethod_6<MedsController>(_fikaPlayer, _foodDrink, _bodyParts, _amount, _animationVariant);
        }

        internal void SendPacket()
        {
            _fikaPlayer.CommonPacket.Type = ECommonSubPacketType.Proceed;
            _fikaPlayer.CommonPacket.SubPacket = ProceedPacket.FromValue(_bodyParts, _foodDrink.Id, _amount, _animationVariant,
                EProceedType.MedsClass, false);
            _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _fikaPlayer.CommonPacket, DeliveryMethod.ReliableOrdered, true);
        }

        internal void HandleResult(IResult result)
        {
            if (result.Succeed)
            {
                ConfirmCallback();
            }
        }
    }

    private class KnifeControllerHandler(FikaPlayer fikaPlayer, KnifeComponent knife)
    {
        private readonly FikaPlayer _fikaPlayer = fikaPlayer;
        public readonly KnifeComponent Knife = knife;
        public Process<KnifeController, IKnifeController> Process;
        public Action ConfirmCallback;

        internal FikaClientKnifeController ReturnController()
        {
            return FikaClientKnifeController.Create(_fikaPlayer, Knife);
        }

        internal void SendPacket()
        {
            _fikaPlayer.CommonPacket.Type = ECommonSubPacketType.Proceed;
            _fikaPlayer.CommonPacket.SubPacket = ProceedPacket.FromValue(default, Knife.Item.Id, 0f, 0, EProceedType.Knife, false);
            _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _fikaPlayer.CommonPacket, DeliveryMethod.ReliableOrdered, true);
        }

        internal void HandleResult(IResult result)
        {
            if (result.Succeed)
            {
                ConfirmCallback();
            }
        }
    }

    private class QuickKnifeControllerHandler(FikaPlayer fikaPlayer, KnifeComponent knife)
    {
        private readonly FikaPlayer _fikaPlayer = fikaPlayer;
        public readonly KnifeComponent Knife = knife;
        public Process<QuickKnifeKickController, GInterface207> Process;
        public Action ConfirmCallback;

        internal QuickKnifeKickController ReturnController()
        {
            return QuickKnifeKickController.smethod_9<QuickKnifeKickController>(_fikaPlayer, Knife);
        }

        internal void SendPacket()
        {
            _fikaPlayer.CommonPacket.Type = ECommonSubPacketType.Proceed;
            _fikaPlayer.CommonPacket.SubPacket = ProceedPacket.FromValue(default, Knife.Item.Id, 0f, 0, EProceedType.QuickKnifeKick, false);
            _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _fikaPlayer.CommonPacket, DeliveryMethod.ReliableOrdered, true);
        }

        internal void HandleResult(IResult result)
        {
            if (result.Succeed)
            {
                ConfirmCallback();
            }
        }
    }

    private class GrenadeControllerHandler(FikaPlayer fikaPlayer, ThrowWeapItemClass throwWeap)
    {
        private readonly FikaPlayer _fikaPlayer = fikaPlayer;
        private readonly ThrowWeapItemClass _throwWeap = throwWeap;
        public Process<GrenadeHandsController, IHandsThrowController> Process;
        public Action ConfirmCallback;

        internal FikaClientGrenadeController ReturnController()
        {
            return FikaClientGrenadeController.Create(_fikaPlayer, _throwWeap);
        }

        internal void SendPacket()
        {
            _fikaPlayer.CommonPacket.Type = ECommonSubPacketType.Proceed;
            _fikaPlayer.CommonPacket.SubPacket = ProceedPacket.FromValue(default, _throwWeap.Id, 0f, 0, EProceedType.GrenadeClass, false);
            _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _fikaPlayer.CommonPacket, DeliveryMethod.ReliableOrdered, true);
        }

        internal void HandleResult(IResult result)
        {
            if (result.Succeed)
            {
                ConfirmCallback();
            }
        }
    }

    private class QuickGrenadeControllerHandler(FikaPlayer fikaPlayer, ThrowWeapItemClass throwWeap)
    {
        private readonly FikaPlayer _fikaPlayer = fikaPlayer;
        private readonly ThrowWeapItemClass _throwWeap = throwWeap;
        public Process<QuickGrenadeThrowHandsController, GInterface206> Process;
        public Action ConfirmCallback;

        internal FikaClientQuickGrenadeController ReturnController()
        {
            return FikaClientQuickGrenadeController.Create(_fikaPlayer, _throwWeap);
        }

        internal void SendPacket()
        {
            _fikaPlayer.CommonPacket.Type = ECommonSubPacketType.Proceed;
            _fikaPlayer.CommonPacket.SubPacket = ProceedPacket.FromValue(default, _throwWeap.Id, 0f, 0, EProceedType.QuickGrenadeThrow, false);
            _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _fikaPlayer.CommonPacket, DeliveryMethod.ReliableOrdered, true);
        }

        internal void HandleResult(IResult result)
        {
            if (result.Succeed)
            {
                ConfirmCallback();
            }
        }
    }

    private class DropHandler(FikaPlayer fikaPlayer)
    {
        private readonly FikaPlayer fikaPlayer = fikaPlayer;

        internal void HandleResult()
        {

        }
    }
}
#endregion
