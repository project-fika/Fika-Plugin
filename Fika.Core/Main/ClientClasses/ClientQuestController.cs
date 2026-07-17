using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.Quests;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.Backend;
using Fika.Core.Networking.Packets.Communication;

namespace Fika.Core.Main.ClientClasses;

public class ClientQuestController(Profile profile, InventoryController inventoryController, IPlayerSearchController searchController, IQuestActions session, FikaPlayer player)
    : GClass4007(profile, inventoryController, searchController, session)
{
    protected readonly FikaPlayer _player = player;
    protected bool _canSendAndReceive;
    private readonly bool _isClient = FikaBackendUtils.IsClient;
    private bool _sendQuestSync = true;

    /// <summary>
    /// Used to prevent errors when subscribing to the event
    /// </summary>
    public virtual void LateInit()
    {
        _player.Profile.OnItemZoneDropped += Profile_OnItemZoneDropped;
        _player.OnSpecialPlaceVisited += Player_OnSpecialPlaceVisited;
        _player.InventoryController.AddItemEvent += InventoryController_AddItemEvent;
    }

    public void ToggleSend(bool enabled)
    {
        _sendQuestSync = enabled;
    }

    private void InventoryController_AddItemEvent(GEventArgs2 eventArgs)
    {
        if (eventArgs.Status != CommandStatus.Succeed || !_isClient || !_sendQuestSync)
        {
            return;
        }

        if (eventArgs.Item.QuestItem)
        {
            var packet = new QuestSyncPacket
            {
                NetId = _player.NetId,
                Type = QuestSyncPacket.EQuestSyncType.PickUpQuestItem,
                ItemId = eventArgs.Item.TemplateId
            };

            Singleton<FikaClient>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered);
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        _player.Profile.OnItemZoneDropped -= Profile_OnItemZoneDropped;
        _player.OnSpecialPlaceVisited -= Player_OnSpecialPlaceVisited;
        _player.InventoryController.AddItemEvent -= InventoryController_AddItemEvent;
    }

    private void Player_OnSpecialPlaceVisited(string zoneId, int experience)
    {
        if (!_isClient || !_sendQuestSync)
        {
            return;
        }

        var packet = new QuestSyncPacket
        {
            NetId = _player.NetId,
            Type = QuestSyncPacket.EQuestSyncType.PlaceVisited,
            ZoneId = zoneId
        };
        Singleton<FikaClient>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered);
    }

    protected void Profile_OnItemZoneDropped(string itemId, string zoneId)
    {
        if (!_isClient || !_sendQuestSync)
        {
            return;
        }

        var packet = new QuestSyncPacket
        {
            NetId = _player.NetId,
            Type = QuestSyncPacket.EQuestSyncType.ItemDrop,
            ItemId = !string.IsNullOrWhiteSpace(itemId) ? itemId : null,
            ZoneId = zoneId
        };
        Singleton<FikaClient>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered);
    }

    public override void OnConditionValueChanged(QuestClass conditional, EQuestStatus status, Condition condition, bool notify = true)
    {
        base.OnConditionValueChanged(conditional, status, condition, notify);
        if (_isClient && _sendQuestSync)
        {
            var counter = conditional.ConditionCountersManager.GetCounter(condition.id);
            if (counter == null)
            {
                FikaGlobals.LogWarning($"There was no counter for condition [{condition.id}]");
                return;
            }

            var packet = new QuestSyncPacket
            {
                NetId = _player.NetId,
                Type = QuestSyncPacket.EQuestSyncType.Conditional,
                QuestId = conditional.Id.GetHashCode(),
                ConditionId = condition.id,
                Value = counter.Value
            };
            Singleton<FikaClient>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered);
        }
    }

    public override async Task<GStruct154<GStruct426<QuestClass>>> FinishQuest(QuestClass quest, bool runNetworkTransaction)
    {
        List<FlatItemsDataClass[]> items = [];
        var hasRewards = false;
        if (quest.Rewards.TryGetValue(EQuestStatus.Success, out var list))
        {
            hasRewards = true;
            for (var i = 0; i < list.Count; i++)
            {
                var item = list[i];
                if (item.type != ERewardType.Item)
                {
                    continue;
                }
                items.Add(item.items);
            }
        }
        var finishResult = await base.FinishQuest(quest, runNetworkTransaction);
        if (finishResult.Succeeded && hasRewards)
        {
            InRaidQuestPacket packet = new()
            {
                NetId = _player.NetId,
                Type = InRaidQuestPacket.InraidQuestType.Finish,
                Items = items
            };

            _player.PacketSender.NetworkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
        }
        return finishResult;
    }

    public override async Task<IResult> HandoverItem(QuestClass quest, ConditionItem condition, Item[] items, bool runNetworkTransaction)
    {
        List<MongoID> itemIds = [];
        var hasNonQuestItem = false;
        for (var i = 0; i < items.Length; i++)
        {
            var item = items[i];
            if (!item.QuestItem)
            {
                hasNonQuestItem = true;
                break;
            }
        }

        if (hasNonQuestItem)
        {
            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (item.QuestItem)
                {
                    continue;
                }

                itemIds.Add(item.Id);
            }
        }

        var handoverResult = await base.HandoverItem(quest, condition, items, runNetworkTransaction);
        if (handoverResult.Succeed && hasNonQuestItem)
        {

            InRaidQuestPacket packet = new()
            {
                NetId = _player.NetId,
                Type = InRaidQuestPacket.InraidQuestType.Handover,
                ItemIdsToRemove = itemIds
            };

            _player.PacketSender.NetworkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
        }
        return handoverResult;
    }

    internal void ReceiveReconnectQuestSync(List<QuestSyncPacket> packets)
    {
        var shouldReset = _canSendAndReceive;
        _canSendAndReceive = false;
        _sendQuestSync = false;

        for (var i = 0; i < packets.Count; i++)
        {
            var packet = packets[i];
            switch (packet.Type)
            {
                case QuestSyncPacket.EQuestSyncType.Conditional:
                    QuestConditionValueChanged(packet.QuestId, packet.ConditionId, packet.Value);
                    break;
                case QuestSyncPacket.EQuestSyncType.ItemDrop:
                    Profile.ItemDroppedAtPlace(packet.ItemId, packet.ZoneId);
                    break;
                case QuestSyncPacket.EQuestSyncType.PlaceVisited:
                    _player.SpecialPlaceVisited(packet.ZoneId, 0);
                    break;
                case QuestSyncPacket.EQuestSyncType.PickUpQuestItem:
                    LootReconnectQuestItem(packet.ItemId.Value);
                    break;
            }
        }

        _canSendAndReceive = shouldReset;
        _sendQuestSync = true;
    }

    private void LootReconnectQuestItem(MongoID itemId)
    {
        var gameWorld = (FikaClientGameWorld)_player.GameWorld;
        var lootItems = (List<LootItemPositionClass>)typeof(GameWorld)
            .GetField("list_1", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(gameWorld);

#if DEBUG
        FikaGlobals.LogInfo($"Looking for quest item [{itemId}]");
#endif

        for (var i = lootItems.Count - 1; i >= 0; i--)
        {
            var lootItem = lootItems[i];
#if DEBUG
            FikaGlobals.LogInfo($"Scanning quest item [{lootItem.Item.TemplateId}]");
#endif
            if (lootItem.Item.TemplateId == itemId)
            {
                var item = gameWorld.CreateReconnectQuestItem(lootItem, true, _player);
                var playerInventory = _player.InventoryController;
                var pickupResult = InteractionsHandlerClass.QuickFindAppropriatePlace(item, playerInventory,
                    playerInventory.Inventory.Equipment.ToEnumerable(),
                    InteractionsHandlerClass.EMoveItemOrder.PickUp, true);

                if (pickupResult.Succeeded && playerInventory.CanExecute(pickupResult.Value))
                {
                    playerInventory.RunNetworkTransaction(pickupResult.Value);
                }
                else
                {
                    FikaGlobals.LogError($"There was an error when looting the quest item [{item.LocalizedShortName()}] during resync: {pickupResult.Error}");
                }

                lootItems.RemoveAt(i);

                return;
            }
        }

        FikaGlobals.LogError($"Could not find item [{itemId}] during resync");
    }

    public void QuestConditionValueChanged(int questId, MongoID conditionId, double value)
    {
        var conditional = ConditionalBook.GetConditional(questId);
        if (conditional == null)
        {
            FikaGlobals.LogError("Quest with id (" + questId.ToString() + ") is null!");
            return;
        }
        var counter = conditional.ConditionCountersManager.GetCounter(conditionId);
        if (counter != null)
        {
            counter.Value = (int)value;
            return;
        }

        FikaGlobals.LogError($"Could not find counter [{conditionId}]");
    }
}
