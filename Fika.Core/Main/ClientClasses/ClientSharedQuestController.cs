using EFT.Communications;
using System;
using System.Collections.Generic;
using System.Linq;
using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using EFT.Quests;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Packets.Communication;
using static Fika.Core.UI.FikaUIGlobals;
using Diz.Utils;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace Fika.Core.Main.ClientClasses;

public sealed class ClientSharedQuestController : ClientQuestController
{
    public ClientSharedQuestController(IntPtr pointer) : base(pointer)
    {
    }

    public ClientSharedQuestController(Profile profile, InventoryController inventoryController, IPlayerSearchController searchController, IQuestSession session, FikaPlayer player) : base(ClassInjector.DerivedConstructorPointer<ClientSharedQuestController>(), profile, inventoryController, searchController, session, player)
    {
    }

    private readonly List<string> _lastFromNetwork = [];
    private readonly HashSet<string> _acceptedTypes = [];
    private readonly HashSet<string> _lootedTemplateIds = [];
    private readonly HashSet<EGroupQuestCounterEvent> _relayedEvents = [];
    private readonly HashSet<EGroupQuestCounterEvent> _partyEvents = [];
    private readonly HashSet<int> _relayNotified = [];

    private bool _isItemBeingDropped;
    private bool _relayCoversChange;
    private string _relaySource;
    private string _relayTarget;

    public override void Init()
    {
        _canSendAndReceive = true;
        base.Init();

        foreach (EGroupQuestCounterEvent counterEvent in Enum.GetValues(typeof(EGroupQuestCounterEvent)))
        {
            if (GroupQuestSettingsProvider.IsCounterEventEnabledForParty(counterEvent))
            {
                _partyEvents.Add(counterEvent);
                _relayedEvents.Add(counterEvent);
            }
        }

        var settings = FikaPlugin.Instance.Settings;
        if (!settings.SharedQuestProgression)
        {
            return;
        }

        if (settings.EasyKillConditions.Value)
        {
            _relayedEvents.Remove(EGroupQuestCounterEvent.Kill);
            _relayedEvents.Remove(EGroupQuestCounterEvent.Shot);
        }

        var array = (FikaPlugin.EQuestSharingTypes[])Enum.GetValues(typeof(FikaPlugin.EQuestSharingTypes));
        for (var i = 0; i < array.Length; i++)
        {
            var shareType = array[i];
            if (settings.QuestTypesToShareAndReceive.Value.HasFlag(shareType))
            {
                switch (shareType)
                {
                    case FikaPlugin.EQuestSharingTypes.Kills:
                        if (!settings.EasyKillConditions.Value)
                        {
                            _acceptedTypes.Add("Elimination");
                            _acceptedTypes.Add(shareType.ToString());
                            _relayedEvents.Add(EGroupQuestCounterEvent.Kill);
                            _relayedEvents.Add(EGroupQuestCounterEvent.Shot);
                        }
                        break;
                    case FikaPlugin.EQuestSharingTypes.Item:
                        _acceptedTypes.Add("FindItem");
                        _relayedEvents.Add(EGroupQuestCounterEvent.UseItem);
                        break;
                    case FikaPlugin.EQuestSharingTypes.Location:
                        _acceptedTypes.Add("Exploration");
                        _acceptedTypes.Add("Discover");
                        _acceptedTypes.Add("VisitPlace");
                        _acceptedTypes.Add(shareType.ToString());
                        _relayedEvents.Add(EGroupQuestCounterEvent.TriggerCondition);
                        _relayedEvents.Add(EGroupQuestCounterEvent.LaunchFlare);
                        break;
                    case FikaPlugin.EQuestSharingTypes.PlaceBeacon:
                        _acceptedTypes.Add(shareType.ToString());
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Used to prevent errors when subscribing to the event
    /// </summary>
    public override void LateInit()
    {
        base.LateInit();
        if (_acceptedTypes.Contains("PlaceBeacon"))
        {
            _player.Profile.OnItemZoneDropped += Shared_Profile_OnItemZoneDropped;
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        _player.Profile.OnItemZoneDropped -= Shared_Profile_OnItemZoneDropped;
    }

    private void Shared_Profile_OnItemZoneDropped(string itemId, string zoneId)
    {
        if (!_canSendAndReceive)
        {
            return;
        }

        if (_isItemBeingDropped)
        {
            return;
        }

        QuestDropItemPacket packet = new(_player.Profile.Info.MainProfileNickname, itemId, zoneId);
#if DEBUG
        FikaGlobals.LogInfo($"Profile_OnItemZoneDropped: Sending quest progress itemId: {itemId} zoneId: {zoneId}");
#endif
        _player.PacketSender.NetworkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
    }

    public override void OnConditionValueChanged(Quest conditional, EQuestStatus status, Condition condition, bool notify = true)
    {
        base.OnConditionValueChanged(conditional, status, condition, notify);
        if (!_canSendAndReceive)
        {
            return;
        }

        if (_relaySource != null)
        {
            NotifyRelayed(conditional);
            return;
        }

        if (_relayCoversChange)
        {
            return;
        }

        if (_lastFromNetwork.Contains(condition.id))
        {
            _lastFromNetwork.Remove(condition.id);
            return;
        }
        SendQuestPacket(conditional, condition);
    }

    public override bool IsGroupQuestCounterEventEnabledForPartyRelay(EGroupQuestCounterEvent counterEvent)
    {
        return _canSendAndReceive && _relayedEvents.Contains(counterEvent);
    }

    public override void RelayPartySharedConditionProgress(EGroupQuestCounterEvent counterEvent, int valueToAdd, Il2CppReferenceArray<ConditionCheck> checks)
    {
        if (!QuestRelay.TryBuild(_player.Profile.Info.MainProfileNickname, counterEvent, valueToAdd, checks, _relayTarget, out var packet))
        {
            return;
        }

#if DEBUG
        FikaGlobals.LogInfo($"RelayPartySharedConditionProgress: Relaying {counterEvent} with {packet.Checks.Count} checks");
#endif
        _player.PacketSender.NetworkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
    }

    public override void CheckKillConditionCounter(string target, string enemyProfileId, Il2CppSystem.Collections.Generic.List<string> targetEquipment,
        Item weapon, EBodyPart bodyPart, string locationId, float distance, string role, int hour, HealthEffects enemyEffects,
        HealthEffects effects, Il2CppSystem.Collections.Generic.List<string> zoneIds, Il2CppStringArray buffs)
    {
        _relayCoversChange = IsGroupQuestCounterEventEnabledForPartyRelay(EGroupQuestCounterEvent.Kill);
        try
        {
            base.CheckKillConditionCounter(target, enemyProfileId, targetEquipment, weapon, bodyPart, locationId,
                distance, role, hour, enemyEffects, effects, zoneIds, buffs);
        }
        finally
        {
            _relayCoversChange = false;
        }
    }

    public override void CheckShotConditionCounter(string target, string enemyProfileId, Il2CppSystem.Collections.Generic.List<string> targetEquipment,
        Item weapon, EBodyPart bodyPart, string locationId, float distance, string role, int hour, HealthEffects enemyEffects,
        HealthEffects effects, Il2CppSystem.Collections.Generic.List<string> zoneIds, Il2CppStringArray buffs)
    {
        _relayCoversChange = IsGroupQuestCounterEventEnabledForPartyRelay(EGroupQuestCounterEvent.Shot);
        try
        {
            base.CheckShotConditionCounter(target, enemyProfileId, targetEquipment, weapon, bodyPart, locationId,
                distance, role, hour, enemyEffects, effects, zoneIds, buffs);
        }
        finally
        {
            _relayCoversChange = false;
        }
    }

    public override void CheckUseItemCounter(string target, float count, string locationId, Il2CppSystem.Collections.Generic.List<string> zoneIds)
    {
        _relayCoversChange = IsGroupQuestCounterEventEnabledForPartyRelay(EGroupQuestCounterEvent.UseItem);
        try
        {
            base.CheckUseItemCounter(target, count, locationId, zoneIds);
        }
        finally
        {
            _relayCoversChange = false;
        }
    }

    public override void CheckShootFlareCounter(string zoneId)
    {
        _relayCoversChange = IsGroupQuestCounterEventEnabledForPartyRelay(EGroupQuestCounterEvent.LaunchFlare);
        try
        {
            base.CheckShootFlareCounter(zoneId);
        }
        finally
        {
            _relayCoversChange = false;
        }
    }

    /// <summary>
    /// The trigger checks hash <paramref name="target"/> into their identity without keeping it as a test value,
    /// so it has to be picked up here for the relay to rebuild them.
    /// </summary>
    public override void CheckTriggerConditionCounter(string target, HealthEffects effects, string locationId)
    {
        _relayTarget = target;
        _relayCoversChange = IsGroupQuestCounterEventEnabledForPartyRelay(EGroupQuestCounterEvent.TriggerCondition);
        try
        {
            base.CheckTriggerConditionCounter(target, effects, locationId);
        }
        finally
        {
            _relayCoversChange = false;
            _relayTarget = null;
        }
    }

    internal void ReceiveQuestRelayPacket(QuestRelayPacket packet)
    {
        if (!_canSendAndReceive || !IsGroupQuestCounterEventEnabledForPartyRelay(packet.CounterEvent))
        {
            return;
        }

#if DEBUG
        FikaGlobals.LogInfo($"ReceiveQuestRelayPacket: Applying {packet.CounterEvent} from {packet.Nickname}");
#endif
        var checks = QuestRelay.ToIl2Cpp(packet.Checks);
        _relayNotified.Clear();
        _relaySource = packet.Nickname;
        try
        {
            // Both paths skip counters flagged isNotGroupProgress
            if (_partyEvents.Contains(packet.CounterEvent))
            {
                ConditionalBook.TestGroupProgressConditions(packet.ValueToAdd, checks);
            }
            else
            {
                for (var i = 0; i < Quests.Count; i++)
                {
                    var quest = Quests[i];
                    if (IsQuestActive(quest) && !IsStory(quest))
                    {
                        quest.ConditionCountersManager.TestFromGroupPartyMember(packet.ValueToAdd, checks);
                    }
                }
            }
        }
        finally
        {
            _relaySource = null;
        }
    }

    private void NotifyRelayed(Quest quest)
    {
        if (!FikaPlugin.Instance.Settings.QuestSharingNotifications.Value || !_relayNotified.Add(quest.Id.GetHashCode()))
        {
            return;
        }

        FikaGlobals.DisplayMessage(string.Format(LocaleUtils.RECEIVED_SHARED_QUEST_PROGRESS.Localized(),
            [ColorizeText(EColor.GREEN, _relaySource), ColorizeText(EColor.BROWN, quest.Template.Name)]),
            iconType: EFT.Communications.ENotificationIconType.Quest);
    }

    public bool ContainsAcceptedType(string type)
    {
        return _acceptedTypes.Contains(type);
    }

    public void AddNetworkId(string id)
    {
        if (!_lastFromNetwork.Contains(id))
        {
            _lastFromNetwork.Add(id);
        }
    }

    public void AddLootedTemplateId(string templateId)
    {
        if (!_lootedTemplateIds.Contains(templateId))
        {
            _lootedTemplateIds.Add(templateId);
        }
    }

    public bool CheckForTemplateId(string templateId)
    {
        return _lootedTemplateIds.Contains(templateId);
    }

    public void ToggleQuestSharing(bool state)
    {
        _canSendAndReceive = state;
    }

    private void SendQuestPacket(IConditional conditional, Condition condition)
    {
        if (!_canSendAndReceive)
        {
            return;
        }

        if (conditional is Quest quest && !IsStory(quest))
        {
            var counter = quest.ConditionCountersManager.GetCounter(condition.id);
            if (counter != null)
            {
                if (!ValidateQuestType(counter))
                {
                    return;
                }

                QuestConditionPacket packet = new()
                {
                    Nickname = _player.Profile.Info.MainProfileNickname,
                    Id = counter.Id,
                    SourceId = counter.SourceId
                };
#if DEBUG
                FikaGlobals.LogInfo("SendQuestPacket: Sending quest progress");
#endif
                _player.PacketSender.NetworkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
            }
        }
    }

    internal void ReceiveQuestPacket(QuestConditionPacket packet)
    {
        if (!_canSendAndReceive)
        {
            return;
        }

        AddNetworkId(packet.Id);
        for (var i = 0; i < Quests.Count; i++)
        {
            var quest = Quests[i];
            // Extra check to prevent redundant notifications
            if (!IsQuestActive(quest) || IsStory(quest))
            {
                continue;
            }

            if (quest.Id == packet.SourceId)
            {
#if DEBUG
                FikaGlobals.LogInfo($"Quest id matched sourceId, status: {quest.QuestStatus}, name: {quest.Template.Name.ParseLocalization()}");
#endif
                var counter = quest.ConditionCountersManager.GetCounter(packet.Id);
                if (counter != null && !quest.CompletedConditions.Contains(counter.Template))
                {
                    if (!ValidateQuestType(counter))
                    {
#if DEBUG
                        FikaGlobals.LogInfo($"Failed to verify quest type for {quest.Template.Name.ParseLocalization()}");
#endif
                        return;
                    }

                    counter.Value++;
                    if (FikaPlugin.Instance.Settings.QuestSharingNotifications.Value)
                    {
                        FikaGlobals.DisplayMessage(string.Format(LocaleUtils.RECEIVED_SHARED_QUEST_PROGRESS.Localized(),
                            [ColorizeText(EColor.GREEN, packet.Nickname), ColorizeText(EColor.BROWN, quest.Template.Name)]),
                            iconType: EFT.Communications.ENotificationIconType.Quest);
                    }
                }
            }
        }
    }

    internal void ReceiveQuestItemPacket(QuestItemPacket packet)
    {
        if (!_canSendAndReceive || !_acceptedTypes.Contains("FindItem"))
        {
            return;
        }

        if (packet.ItemId is not null && WantsSharedItem(packet.ItemId))
        {
            var item = _player.FindQuestItem(packet.ItemId);
            if (item != null)
            {
                var playerInventory = _player.InventoryController;
                var pickupResult = ItemManipulator.QuickFindAppropriatePlace(item, playerInventory,
                    playerInventory.Inventory.Equipment.AsIl2CppEnumerable<CompoundItem>(),
                    ItemManipulator.EMoveItemOrder.PickUp, true);

                if (pickupResult.Succeeded && playerInventory.CanExecute(pickupResult.Value))
                {
                    AddLootedTemplateId(item.TemplateId);
                    playerInventory.RunNetworkTransaction(pickupResult.Value);
                    if (FikaPlugin.Instance.Settings.QuestSharingNotifications.Value)
                    {
                        FikaGlobals.DisplayMessage(string.Format(LocaleUtils.RECEIVED_SHARED_ITEM_PICKUP.Localized(),
                            [ColorizeText(EColor.GREEN, packet.Nickname), ColorizeText(EColor.BLUE, item.Name.Localized())]),
                                            iconType: EFT.Communications.ENotificationIconType.Quest);
                    }
                }
            }
        }
    }

    internal void ReceiveQuestDropItemPacket(QuestDropItemPacket packet)
    {
        if (!_canSendAndReceive)
        {
            return;
        }

        if (!_acceptedTypes.Contains("PlaceBeacon"))
        {
            return;
        }

        _isItemBeingDropped = true;
        string itemId = packet.ItemId;
        var zoneId = packet.ZoneId;

        if (!HasQuestForItem(itemId, zoneId, out var questName))
        {
            _isItemBeingDropped = false;
#if DEBUG
            FikaGlobals.LogInfo($"Did not have quest for item {itemId}, zoneId {zoneId}");
#endif
            return;
        }

#if DEBUG
        FikaGlobals.LogInfo($"Had quest for item {itemId}, zoneId {zoneId}");
#endif

        if (FikaPlugin.Instance.Settings.QuestSharingNotifications.Value)
        {
            FikaGlobals.DisplayMessage(string.Format(LocaleUtils.RECEIVED_SHARED_ITEM_PLANT.Localized(),
                [ColorizeText(EColor.GREEN, packet.Nickname), ColorizeText(EColor.BROWN, questName)]),
                                iconType: EFT.Communications.ENotificationIconType.Quest);
        }

        foreach (var questItem in _player.Inventory.QuestRaidItems.GetAllItems())
        {
            if (questItem.TemplateId == itemId && questItem.QuestItem)
            {
                var removeResult = ItemManipulator.Remove(questItem, _player.InventoryController, true);
                _player.InventoryController.TryRunNetworkTransaction(removeResult, new System.Action<Comfort.Common.IResult>(result =>
                {
                    if (!result.Succeed)
                    {
                        FikaGlobals.LogError("ReceiveQuestDropItemPacket: Discard failed: " + result.Error);
                    }
                }));
            }
        }
        _player.Profile.ItemDroppedAtPlace(itemId, zoneId);
        _isItemBeingDropped = false;
    }

    private bool IsQuestActive(Quest quest)
    {
        return quest.QuestStatus is EQuestStatus.Started;
    }

    /// <summary>
    /// Story quests drive each player's own chapter progress, so nothing of theirs is shared
    /// </summary>
    private static bool IsStory(Quest quest)
    {
        return quest.Template != null && quest.Template.IsMainQuest;
    }

    /// <summary>
    /// Whether a teammate's pickup should pull this player's copy of the item. It needs an unfinished FindItem or
    /// HandoverItem on an active quest, and no story quest may use the item, since holding it can start or advance a chapter
    /// </summary>
    private bool WantsSharedItem(MongoID templateId)
    {
        var id = templateId.ToString();
        var wanted = false;
        for (var i = 0; i < Quests.Count; i++)
        {
            var quest = Quests[i];
            foreach (var conditions in quest.Conditions)
            {
                foreach (var condition in conditions.Value)
                {
                    if (!Targets(condition, id))
                    {
                        continue;
                    }

                    if (IsStory(quest))
                    {
                        return false;
                    }

                    if (IsQuestActive(quest) && !quest.CompletedConditions.Contains(condition)
                        && (condition.TryCast<ConditionFindItem>() != null || condition.TryCast<ConditionHandoverItem>() != null))
                    {
                        wanted = true;
                    }
                }
            }
        }

        return wanted;
    }

    private static bool Targets(Condition condition, string id)
    {
        var multiple = condition.TryCast<ConditionMultipleTargets>();
        if (multiple != null)
        {
            return multiple.target != null && multiple.target.Contains(id);
        }

        var single = condition.TryCast<ConditionOneTarget>();
        return single != null && single.target == id;
    }

    private bool HasQuestForItem(string itemId, string zoneId, out string questName)
    {
        for (var i = 0; i < Quests.Count; i++)
        {
            var quest = Quests[i];
            if (!IsQuestActive(quest) || IsStory(quest))
            {
                continue;
            }

            foreach (var conditionPlaceBeacon in quest.GetConditions<ConditionPlaceBeacon>(EQuestStatus.AvailableForFinish))
            {
                if (conditionPlaceBeacon.target.Contains(itemId) && conditionPlaceBeacon.zoneIds.Contains(zoneId))
                {
                    if (!quest.CompletedConditions.Contains(conditionPlaceBeacon) && quest.CheckVisibilityStatus(conditionPlaceBeacon))
                    {
#if DEBUG
                        FikaGlobals.LogWarning($"Found quest for Placed Beacon, itemId: {itemId}, zoneId: {zoneId}, quest: {quest.Template.Name}");
#endif
                        questName = quest.Template.Name;
                        return true;
                    }
#if DEBUG
                    else
                    {
                        FikaGlobals.LogWarning($"Found quest for Placed Beacon, itemId: {itemId}, zoneId: {zoneId}, quest: {quest.Template.Name}, but it was COMPLETED");
                    }
#endif
                }
            }

            foreach (var conditionLeaveItemAtLocation in quest.GetConditions<ConditionLeaveItemAtLocation>(EQuestStatus.AvailableForFinish))
            {
                if (conditionLeaveItemAtLocation.target.Contains(itemId) && conditionLeaveItemAtLocation.zoneIds.Contains(zoneId))
                {
                    if (!quest.CompletedConditions.Contains(conditionLeaveItemAtLocation) && quest.CheckVisibilityStatus(conditionLeaveItemAtLocation))
                    {
#if DEBUG
                        FikaGlobals.LogWarning($"Found quest for Placed Item, itemId: {itemId}, zoneId: {zoneId}, quest: {quest.Template.Name}");
#endif
                        questName = quest.Template.Name;
                        return true;
                    }
#if DEBUG
                    else
                    {
                        FikaGlobals.LogWarning($"Found quest for Placed Item, itemId: {itemId}, zoneId: {zoneId}, quest: {quest.Template.Name}, but it was COMPLETED");
                    }
#endif
                }
            }
        }

#if DEBUG
        FikaGlobals.LogWarning($"Did not have quest for Place Beacon/Item, itemId: {itemId}, zoneId: {zoneId}");
#endif
        questName = null;
        return false;
    }

    /// <summary>
    /// Validates quest typing, some quests use CounterCreator which we also need to validate.
    /// </summary>
    /// <param name="counter">The counter to validate</param>
    /// <returns>Returns true if the quest type is valid, returns false if not</returns>
    private bool ValidateQuestType(TaskConditionCounter counter)
    {
#if DEBUG
        FikaGlobals.LogInfo($"Validating counter of type {counter.Type}");
#endif
        if (_acceptedTypes.Contains(counter.Type))
        {
            return true;
        }

        if (counter.Type == "CounterCreator")
        {
            var CounterCreator = (ConditionCounterCreator)counter.Template;
#if DEBUG
            FikaGlobals.LogInfo($"CounterCreator Type {CounterCreator.type}");
#endif

            if (_acceptedTypes.Contains(CounterCreator.type.ToString()))
            {
                return true;
            }
        }

        return false;
    }
}
