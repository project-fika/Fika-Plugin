using EFT;
using EFT.InventoryLogic;
using EFT.Quests;
using Fika.Core.Coop.Players;
using Fika.Core.Networking.Packets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fika.Core.Coop.ClientClasses
{
    public sealed class CoopClientSharedQuestController(Profile profile, InventoryControllerClass inventoryController,
        IQuestActions session, CoopPlayer player, bool fromServer = true) : LocalQuestControllerClass(profile, inventoryController, session, fromServer)
    {
        private readonly CoopPlayer player = player;
        private readonly List<string> lastFromNetwork = [];
        private readonly HashSet<string> acceptedTypes = [];
        private readonly HashSet<string> lootedTemplateIds = [];
        private readonly HashSet<string> droppedZoneIds = [];
        private bool canSendAndReceive = true;

        public override void Init()
        {
            base.Init();
            foreach (FikaPlugin.EQuestSharingTypes shareType in (FikaPlugin.EQuestSharingTypes[])Enum.GetValues(typeof(FikaPlugin.EQuestSharingTypes)))
            {
                if (FikaPlugin.QuestTypesToShareAndReceive.Value.HasFlag(shareType))
                {
                    switch (shareType)
                    {
                        case FikaPlugin.EQuestSharingTypes.Kill:
                            acceptedTypes.Add("Elimination");
                            acceptedTypes.Add(shareType.ToString());
                            break;
                        case FikaPlugin.EQuestSharingTypes.Item:
                            acceptedTypes.Add("FindItem");
                            break;
                        case FikaPlugin.EQuestSharingTypes.Location:
                            acceptedTypes.Add("Exploration");
                            acceptedTypes.Add("Discover");
                            acceptedTypes.Add("VisitPlace");
                            acceptedTypes.Add(shareType.ToString());
                            break;
                        case FikaPlugin.EQuestSharingTypes.PlaceBeacon:
                            acceptedTypes.Add(shareType.ToString());
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
            if (acceptedTypes.Contains("PlaceBeacon"))
            {
                player.Profile.OnItemZoneDropped += Profile_OnItemZoneDropped;
            }
        }

        private void Profile_OnItemZoneDropped(string itemId, string zoneId)
        {
            if (droppedZoneIds.Contains(itemId))
            {
                return;
            }

            droppedZoneIds.Add(zoneId);
            QuestDropItemPacket packet = new(itemId, zoneId);
#if DEBUG
            FikaPlugin.Instance.FikaLogger.LogInfo("Profile_OnItemZoneDropped: Sending quest progress");
#endif
            player.PacketSender.SendQuestPacket(ref packet);
        }

        public override void OnConditionValueChanged(IConditionCounter conditional, EQuestStatus status, Condition condition, bool notify = true)
        {
            base.OnConditionValueChanged(conditional, status, condition, notify);

            if (!canSendAndReceive)
            {
                return;
            }

            if (lastFromNetwork.Contains(condition.id))
            {
                lastFromNetwork.Remove(condition.id);
                return;
            }
            SendQuestPacket(conditional, condition);
        }

        public void AddNetworkId(string id)
        {
            if (!lastFromNetwork.Contains(id))
            {
                lastFromNetwork.Add(id);
            }
        }

        public void AddLootedTemplateId(string templateId)
        {
            if (!lootedTemplateIds.Contains(templateId))
            {
                lootedTemplateIds.Add(templateId);
            }
        }

        public bool CheckForTemplateId(string templateId)
        {
            return lootedTemplateIds.Contains(templateId);
        }

        public void ToggleQuestSharing(bool state)
        {
            canSendAndReceive = state;
        }

        private void SendQuestPacket(IConditionCounter conditional, Condition condition)
        {
            if (!canSendAndReceive)
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

                    QuestConditionPacket packet = new(player.Profile.Info.MainProfileNickname, counter.Id, counter.SourceId);
#if DEBUG
                    FikaPlugin.Instance.FikaLogger.LogInfo("SendQuestPacket: Sending quest progress");
#endif
                    player.PacketSender.SendQuestPacket(ref packet);
                }
            }
        }

        internal void ReceiveQuestPacket(ref QuestConditionPacket packet)
        {
            if (!canSendAndReceive)
            {
                return;
            }

            AddNetworkId(packet.Id);
            foreach (QuestClass quest in Quests)
            {
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
                            NotificationManagerClass.DisplayMessageNotification($"Received shared quest progression from {packet.Nickname}",
                                                iconType: EFT.Communications.ENotificationIconType.Quest);
                        }
                    }
                }
            }
        }

        internal void ReceiveQuestItemPacket(ref QuestItemPacket packet)
        {
            if (!canSendAndReceive)
            {
                return;
            }

            if (!string.IsNullOrEmpty(packet.ItemId))
            {
                Item item = player.FindItem(packet.ItemId, true);
                if (item != null)
                {
                    InventoryControllerClass playerInventory = player.InventoryControllerClass;
                    GStruct414<GInterface339> pickupResult = InteractionsHandlerClass.QuickFindAppropriatePlace(item, playerInventory,
                        playerInventory.Inventory.Equipment.ToEnumerable(),
                        InteractionsHandlerClass.EMoveItemOrder.PickUp, true);

                    if (pickupResult.Succeeded && playerInventory.CanExecute(pickupResult.Value))
                    {
                        AddLootedTemplateId(item.TemplateId);
                        playerInventory.RunNetworkTransaction(pickupResult.Value);
                        if (FikaPlugin.QuestSharingNotifications.Value)
                        {
                            NotificationManagerClass.DisplayMessageNotification($"{packet.Nickname} picked up {item.Name.Localized()}",
                                                iconType: EFT.Communications.ENotificationIconType.Quest);
                        }
                    }
                }
            }
        }

        internal void ReceiveQuestDropItemPacket(ref QuestDropItemPacket packet)
        {
            if (!canSendAndReceive)
            {
                return;
            }

            string itemId = packet.ItemId;
            string zoneId = packet.ZoneId;

            if (CheckForDroppedItem(itemId, zoneId))
            {
                return;
            }

            droppedZoneIds.Add(zoneId);
            player.Profile.ItemDroppedAtPlace(itemId, zoneId);
        }

        private bool CheckForDroppedItem(string itemId, string zoneId)
        {
            if (player.Profile.EftStats.DroppedItems.Any(x => x.ItemId == itemId && x.ZoneId == zoneId) || droppedZoneIds.Contains(zoneId))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Validates quest typing, some quests use CounterCreator which we also need to validate.
        /// </summary>
        /// <param name="counter">The counter to validate</param>
        /// <returns>Returns true if the quest type is valid, returns false if not</returns>
        internal bool ValidateQuestType(TaskConditionCounterClass counter)
        {
            if (acceptedTypes.Contains(counter.Type))
            {
                return true;
            }

            if (counter.Type == "CounterCreator")
            {
                ConditionCounterCreator CounterCreator = (ConditionCounterCreator)counter.Template;

#if DEBUG
                FikaPlugin.Instance.FikaLogger.LogInfo($"CoopClientSharedQuestController:: ValidateQuestType: CounterCreator Type {CounterCreator.type}");
#endif

                if (acceptedTypes.Contains(CounterCreator.type.ToString()))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
