using System;
using System.Collections.Generic;
using System.Linq;
using EFT;
using EFT.InventoryLogic;
using EFT.Quests;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Packets.Communication;
using static Fika.Core.UI.FikaUIGlobals;

namespace Fika.Core.Main.ClientClasses;

public sealed class ClientSharedQuestController(Profile profile, InventoryController inventoryController,
    IPlayerSearchController searchController, IQuestActions session, FikaPlayer player) : ClientQuestController(profile, inventoryController, searchController, session, player)
{
    private readonly List<string> _lastFromNetwork = [];
    private readonly HashSet<string> _acceptedTypes = [];
    private readonly HashSet<string> _lootedTemplateIds = [];
    private bool _canSendAndReceive = true;
    private bool _isItemBeingDropped;

    public override void Init()
    {
        base.Init();
        var array = (FikaPlugin.EQuestSharingTypes[])Enum.GetValues(typeof(FikaPlugin.EQuestSharingTypes));
        for (var i = 0; i < array.Length; i++)
        {
            var shareType = array[i];
            if (FikaPlugin.Instance.Settings.QuestTypesToShareAndReceive.Value.HasFlag(shareType))
            {
                switch (shareType)
                {
                    case FikaPlugin.EQuestSharingTypes.Kills:
                        if (!FikaPlugin.Instance.Settings.EasyKillConditions.Value)
                        {
                            _acceptedTypes.Add("Elimination");
                            _acceptedTypes.Add(shareType.ToString());
                        }
                        break;
                    case FikaPlugin.EQuestSharingTypes.Item:
                        _acceptedTypes.Add("FindItem");
                        break;
                    case FikaPlugin.EQuestSharingTypes.Location:
                        _acceptedTypes.Add("Exploration");
                        _acceptedTypes.Add("Discover");
                        _acceptedTypes.Add("VisitPlace");
                        _acceptedTypes.Add(shareType.ToString());
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
    public void LateInit()
    {
        if (_acceptedTypes.Contains("PlaceBeacon"))
        {
            _player.Profile.OnItemZoneDropped += Profile_OnItemZoneDropped;
        }
    }

    private void Profile_OnItemZoneDropped(string itemId, string zoneId)
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
        FikaGlobals.LogInfo($"Profile_OnItemZoneDropped: Sending quest progress itemId:{itemId} zoneId:{zoneId}");
#endif
        _player.PacketSender.NetworkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
    }

    public override void OnConditionValueChanged(QuestClass conditional, EQuestStatus status, Condition condition, bool notify = true)
    {
        base.OnConditionValueChanged(conditional, status, condition, notify);
        if (!_canSendAndReceive)
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

        if (conditional is QuestClass quest)
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
            if (!IsQuestActive(quest))
            {
                continue;
            }

            if (quest.Id == packet.SourceId)
            {
#if DEBUG
                FikaGlobals.LogInfo($"Quest id matched sourceId, status: {quest.QuestStatus}, name: {quest.Template.Name.ParseLocalization()}");
#endif
                var counter = quest.ConditionCountersManager.GetCounter(packet.Id);
                if (counter != null && !quest.CompletedConditions.Contains(counter.Id))
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
                        NotificationManagerClass.DisplayMessageNotification(string.Format(LocaleUtils.RECEIVED_SHARED_QUEST_PROGRESS.Localized(),
                            [ColorizeText(EColor.GREEN, packet.Nickname), ColorizeText(EColor.BROWN, quest.Template.Name)]),
                            iconType: EFT.Communications.ENotificationIconType.Quest);
                    }
                }
            }
        }
    }

    internal void ReceiveQuestItemPacket(QuestItemPacket packet)
    {
        if (!_canSendAndReceive)
        {
            return;
        }

        if (packet.ItemId.HasValue)
        {
            var item = _player.FindQuestItem(packet.ItemId.Value);
            if (item != null)
            {
                var playerInventory = _player.InventoryController;
                var pickupResult = InteractionsHandlerClass.QuickFindAppropriatePlace(item, playerInventory,
                    playerInventory.Inventory.Equipment.ToEnumerable(),
                    InteractionsHandlerClass.EMoveItemOrder.PickUp, true);

                if (pickupResult.Succeeded && playerInventory.CanExecute(pickupResult.Value))
                {
                    AddLootedTemplateId(item.TemplateId);
                    playerInventory.RunNetworkTransaction(pickupResult.Value);
                    if (FikaPlugin.Instance.Settings.QuestSharingNotifications.Value)
                    {
                        NotificationManagerClass.DisplayMessageNotification(string.Format(LocaleUtils.RECEIVED_SHARED_ITEM_PICKUP.Localized(),
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
            NotificationManagerClass.DisplayMessageNotification(string.Format(LocaleUtils.RECEIVED_SHARED_ITEM_PLANT.Localized(),
                [ColorizeText(EColor.GREEN, packet.Nickname), ColorizeText(EColor.BROWN, questName)]),
                                iconType: EFT.Communications.ENotificationIconType.Quest);
        }

        foreach (var questItem in _player.Inventory.QuestRaidItems.GetAllItems())
        {
            if (questItem.TemplateId == itemId && questItem.QuestItem)
            {
                var removeResult = InteractionsHandlerClass.Remove(questItem, _player.InventoryController, true);
                _player.InventoryController.TryRunNetworkTransaction(removeResult, result =>
                {
                    if (!result.Succeed)
                    {
                        FikaGlobals.LogError("ReceiveQuestDropItemPacket: Discard failed: " + result.Error);
                    }
                });
            }
        }
        _player.Profile.ItemDroppedAtPlace(itemId, zoneId);
        _isItemBeingDropped = false;
    }

    private bool IsQuestActive(QuestClass quest)
    {
        return quest.QuestStatus is EQuestStatus.Started;
    }

    private bool HasQuestForItem(string itemId, string zoneId, out string questName)
    {
        for (var i = 0; i < Quests.Count; i++)
        {
            var quest = Quests[i];
            if (!IsQuestActive(quest))
            {
                continue;
            }

            foreach (var conditionPlaceBeacon in quest.GetConditions<ConditionPlaceBeacon>(EQuestStatus.AvailableForFinish))
            {
                if (conditionPlaceBeacon.target.Contains(itemId) && conditionPlaceBeacon.zoneId == zoneId)
                {
                    if (!quest.CompletedConditions.Contains(conditionPlaceBeacon.id) && quest.CheckVisibilityStatus(conditionPlaceBeacon))
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
                if (conditionLeaveItemAtLocation.target.Contains(itemId) && conditionLeaveItemAtLocation.zoneId == zoneId)
                {
                    if (!quest.CompletedConditions.Contains(conditionLeaveItemAtLocation.id) && quest.CheckVisibilityStatus(conditionLeaveItemAtLocation))
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
    private bool ValidateQuestType(TaskConditionCounterClass counter)
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
