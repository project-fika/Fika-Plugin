using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.Quests;
using Fika.Core.Coop.Players;
using Fika.Core.Networking;
using Fika.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using static Fika.Core.UI.FikaUIGlobals;

namespace Fika.Core.Coop.ClientClasses
{
    public sealed class CoopClientSharedQuestController(Profile profile, InventoryController inventoryController,
        IPlayerSearchController searchController, IQuestActions session, CoopPlayer player) : CoopClientQuestController(profile, inventoryController, searchController, session, player)
    {
        private readonly List<string> _lastFromNetwork = [];
        private readonly HashSet<string> _acceptedTypes = [];
        private readonly HashSet<string> _lootedTemplateIds = [];
        private bool _canSendAndReceive = true;
        private bool _isItemBeingDropped = false;

        public override void Init()
        {
            base.Init();
            foreach (FikaPlugin.EQuestSharingTypes shareType in (FikaPlugin.EQuestSharingTypes[])Enum.GetValues(typeof(FikaPlugin.EQuestSharingTypes)))
            {
                if (FikaPlugin.QuestTypesToShareAndReceive.Value.HasFlag(shareType))
                {
                    switch (shareType)
                    {
                        case FikaPlugin.EQuestSharingTypes.Kills:
                            if (!FikaPlugin.EasyKillConditions.Value)
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
            FikaPlugin.Instance.FikaLogger.LogInfo($"Profile_OnItemZoneDropped: Sending quest progress itemId:{itemId} zoneId:{zoneId}");
#endif
            _player.PacketSender.SendPacket(ref packet);
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
                TaskConditionCounterClass counter = quest.ConditionCountersManager.GetCounter(condition.id);
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
                    FikaPlugin.Instance.FikaLogger.LogInfo("SendQuestPacket: Sending quest progress");
#endif
                    _player.PacketSender.SendPacket(ref packet);
                }
            }
        }

        internal void ReceiveQuestPacket(ref QuestConditionPacket packet)
        {
            if (!_canSendAndReceive)
            {
                return;
            }

            AddNetworkId(packet.Id);
            foreach (QuestClass quest in Quests)
            {
                // Extra check to prevent redundant notifications
                if (quest.IsDone())
                {
                    continue;
                }

                if (quest.Id == packet.SourceId && quest.QuestStatus == EQuestStatus.Started)
                {
                    TaskConditionCounterClass counter = quest.ConditionCountersManager.GetCounter(packet.Id);
                    if (counter != null && !quest.CompletedConditions.Contains(counter.Id))
                    {
                        if (!ValidateQuestType(counter))
                        {
                            return;
                        }

                        counter.Value++;
                        if (FikaPlugin.QuestSharingNotifications.Value)
                        {
                            NotificationManagerClass.DisplayMessageNotification(string.Format(LocaleUtils.RECEIVED_SHARED_QUEST_PROGRESS.Localized(),
                                [ColorizeText(EColor.GREEN, packet.Nickname), ColorizeText(EColor.BROWN, quest.Template.Name)]),
                                iconType: EFT.Communications.ENotificationIconType.Quest);
                        }
                    }
                }
            }
        }

        internal void ReceiveQuestItemPacket(ref QuestItemPacket packet)
        {
            if (!_canSendAndReceive)
            {
                return;
            }

            if (!string.IsNullOrEmpty(packet.ItemId))
            {
                Item item = _player.FindQuestItem(packet.ItemId);
                if (item != null)
                {
                    InventoryController playerInventory = _player.InventoryController;
                    GStruct459<GInterface407> pickupResult = InteractionsHandlerClass.QuickFindAppropriatePlace(item, playerInventory,
                        playerInventory.Inventory.Equipment.ToEnumerable(),
                        InteractionsHandlerClass.EMoveItemOrder.PickUp, true);

                    if (pickupResult.Succeeded && playerInventory.CanExecute(pickupResult.Value))
                    {
                        AddLootedTemplateId(item.TemplateId);
                        playerInventory.RunNetworkTransaction(pickupResult.Value);
                        if (FikaPlugin.QuestSharingNotifications.Value)
                        {
                            NotificationManagerClass.DisplayMessageNotification(string.Format(LocaleUtils.RECEIVED_SHARED_ITEM_PICKUP.Localized(),
                                [ColorizeText(EColor.GREEN, packet.Nickname), ColorizeText(EColor.BLUE, item.Name.Localized())]),
                                                iconType: EFT.Communications.ENotificationIconType.Quest);
                        }
                    }
                }
            }
        }

        internal void ReceiveQuestDropItemPacket(ref QuestDropItemPacket packet)
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
            string zoneId = packet.ZoneId;

            if (!HasQuestForItem(itemId, zoneId, out string questName))
            {
                _isItemBeingDropped = false;
                return;
            }

            if (FikaPlugin.QuestSharingNotifications.Value)
            {
                NotificationManagerClass.DisplayMessageNotification(string.Format(LocaleUtils.RECEIVED_SHARED_ITEM_PLANT.Localized(),
                    [ColorizeText(EColor.GREEN, packet.Nickname), ColorizeText(EColor.BROWN, questName)]),
                                    iconType: EFT.Communications.ENotificationIconType.Quest);
            }

            foreach (Item questItem in _player.Inventory.QuestRaidItems.GetAllItems())
            {
                if (questItem.TemplateId == itemId && questItem.QuestItem)
                {
                    GStruct459<GClass3279> removeResult = InteractionsHandlerClass.Remove(questItem, _player.InventoryController, true);
                    _player.InventoryController.TryRunNetworkTransaction(removeResult, (IResult result) =>
                    {
                        if (!result.Succeed)
                        {
                            FikaPlugin.Instance.FikaLogger.LogError("ReceiveQuestDropItemPacket: Discard failed: " + result.Error);
                        }
                    });
                }
            }
            _player.Profile.ItemDroppedAtPlace(itemId, zoneId);
            _isItemBeingDropped = false;
        }

        private bool HasQuestForItem(string itemId, string zoneId, out string questName)
        {
            foreach (QuestClass quest in Quests)
            {
                if (quest.IsDone())
                {
                    continue;
                }

                foreach (ConditionPlaceBeacon conditionPlaceBeacon in quest.GetConditions<ConditionPlaceBeacon>(EQuestStatus.AvailableForFinish))
                {
                    if (conditionPlaceBeacon.target.Contains(itemId) && conditionPlaceBeacon.zoneId == zoneId)
                    {
                        if (!quest.CompletedConditions.Contains(conditionPlaceBeacon.id) && quest.CheckVisibilityStatus(conditionPlaceBeacon))
                        {
#if DEBUG
                            FikaPlugin.Instance.FikaLogger.LogWarning($"Found quest for Placed Beacon, itemId: {itemId}, zoneId: {zoneId}, quest: {quest.Template.Name}");
#endif
                            questName = quest.Template.Name;
                            return true;
                        }
#if DEBUG
                        else
                        {
                            FikaPlugin.Instance.FikaLogger.LogWarning($"Found quest for Placed Beacon, itemId: {itemId}, zoneId: {zoneId}, quest: {quest.Template.Name}, but it was COMPLETED");
                        }
#endif
                    }
                }

                foreach (ConditionLeaveItemAtLocation conditionLeaveItemAtLocation in quest.GetConditions<ConditionLeaveItemAtLocation>(EQuestStatus.AvailableForFinish))
                {
                    if (conditionLeaveItemAtLocation.target.Contains(itemId) && conditionLeaveItemAtLocation.zoneId == zoneId)
                    {
                        if (!quest.CompletedConditions.Contains(conditionLeaveItemAtLocation.id) && quest.CheckVisibilityStatus(conditionLeaveItemAtLocation))
                        {
#if DEBUG
                            FikaPlugin.Instance.FikaLogger.LogWarning($"Found quest for Placed Item, itemId: {itemId}, zoneId: {zoneId}, quest: {quest.Template.Name}");
#endif
                            questName = quest.Template.Name;
                            return true;
                        }
#if DEBUG
                        else
                        {
                            FikaPlugin.Instance.FikaLogger.LogWarning($"Found quest for Placed Item, itemId: {itemId}, zoneId: {zoneId}, quest: {quest.Template.Name}, but it was COMPLETED");
                        }
#endif
                    }
                }
            }

#if DEBUG
            FikaPlugin.Instance.FikaLogger.LogWarning($"Did not have quest for Place Beacon/Item, itemId: {itemId}, zoneId: {zoneId}");
#endif
            questName = null;
            return false;
        }

        /// <summary>
        /// Validates quest typing, some quests use CounterCreator which we also need to validate.
        /// </summary>
        /// <param name="counter">The counter to validate</param>
        /// <returns>Returns true if the quest type is valid, returns false if not</returns>
        internal bool ValidateQuestType(TaskConditionCounterClass counter)
        {
            if (_acceptedTypes.Contains(counter.Type))
            {
                return true;
            }

            if (counter.Type == "CounterCreator")
            {
                ConditionCounterCreator CounterCreator = (ConditionCounterCreator)counter.Template;

#if DEBUG
                FikaPlugin.Instance.FikaLogger.LogInfo($"CoopClientSharedQuestController::ValidateQuestType: CounterCreator Type {CounterCreator.type}");
#endif

                if (_acceptedTypes.Contains(CounterCreator.type.ToString()))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
