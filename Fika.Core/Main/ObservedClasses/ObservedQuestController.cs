using Diz.LanguageExtensions;
using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.Quests;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Packets.Backend;
using Fika.Core.Networking.Packets.Communication;

namespace Fika.Core.Main.ObservedClasses;

public class ObservedQuestController(Profile profile, InventoryController inventoryController, IPlayerSearchController searchController, IQuestSession session)
    : QuestControllerClientLocalGame(profile, inventoryController, searchController, session)
{
    private Dictionary<int, List<QuestInformation>> _quests;
    private List<ZoneDropInformation> _zoneDrops;
    private List<string> _visitedPlaces;
    private List<MongoID> _lootedQuestItems;

    public override void Run()
    {
        if (FikaBackendUtils.IsServer)
        {
            _quests = [];
            _zoneDrops = [];
            _visitedPlaces = [];
            _lootedQuestItems = [];
        }
    }

    public void UpdateQuestStatusForClient(QuestSyncPacket packet)
    {
        if (packet.Type is QuestSyncPacket.EQuestSyncType.ItemDrop)
        {
#if DEBUG
            FikaGlobals.LogInfo($"Received item drop quest status from client for zone [{packet.ZoneId}]");
#endif
            UpdateClientZoneDrop(packet.ItemId, packet.ZoneId);
            return;
        }

        if (packet.Type is QuestSyncPacket.EQuestSyncType.PlaceVisited)
        {
#if DEBUG
            FikaGlobals.LogInfo($"Received visited place for zone [{packet.ZoneId}]");
#endif
            _visitedPlaces.Add(packet.ZoneId);
            return;
        }

        if (packet.Type is QuestSyncPacket.EQuestSyncType.PickUpQuestItem)
        {
#if DEBUG
            FikaGlobals.LogInfo($"Received loot quest item [{packet.ItemId.Value}]");
#endif
            _lootedQuestItems.Add(packet.ItemId.Value);
            return;
        }

#if DEBUG
        FikaGlobals.LogInfo($"Received from client for quest [{packet.QuestId}] for conditional [{packet.ConditionId}] with a value of [{packet.Value}]");
#endif

        if (!_quests.TryGetValue(packet.QuestId, out var questInformation))
        {
            questInformation = [];
            _quests.Add(packet.QuestId, questInformation);
        }

        for (var i = 0; i < questInformation.Count; i++)
        {
            var quest = questInformation[i];
            if (quest.ConditionId == packet.ConditionId)
            {
                quest.Value = packet.Value;

                return;
            }
        }

        questInformation.Add(new QuestInformation
        {
            ConditionId = packet.ConditionId,
            Value = packet.Value
        });
    }

    private void UpdateClientZoneDrop(MongoID? itemId, string zoneId)
    {
        _zoneDrops.Add(new ZoneDropInformation(itemId, zoneId));
    }

    public bool TryGetReconnectQuestSyncPackets(out List<QuestSyncPacket> packets)
    {
        if (_quests.Count == 0 && _zoneDrops.Count == 0 && _visitedPlaces.Count == 0 && _lootedQuestItems.Count == 0)
        {
            packets = null;
            return false;
        }

        packets = new List<QuestSyncPacket>(_quests.Count + _zoneDrops.Count + _visitedPlaces.Count + _lootedQuestItems.Count);
        foreach ((var questId, var questInformation) in _quests)
        {
            foreach (var quest in questInformation)
            {
                packets.Add(new QuestSyncPacket
                {
                    Type = QuestSyncPacket.EQuestSyncType.Conditional,
                    QuestId = questId,
                    ConditionId = quest.ConditionId,
                    Value = quest.Value
                });
            }
        }

        for (var i = 0; i < _zoneDrops.Count; i++)
        {
            var zoneDrop = _zoneDrops[i];
            packets.Add(new QuestSyncPacket
            {
                Type = QuestSyncPacket.EQuestSyncType.ItemDrop,
                ItemId = zoneDrop.ItemId,
                ZoneId = zoneDrop.ZoneId
            });
        }

        for (var i = 0; i < _visitedPlaces.Count; i++)
        {
            packets.Add(new QuestSyncPacket
            {
                Type = QuestSyncPacket.EQuestSyncType.PlaceVisited,
                ZoneId = _visitedPlaces[i]
            });
        }

        for (var i = 0; i < _lootedQuestItems.Count; i++)
        {
            packets.Add(new QuestSyncPacket
            {
                Type = QuestSyncPacket.EQuestSyncType.PickUpQuestItem,
                ItemId = _lootedQuestItems[i]
            });
        }

        return true;
    }

    public override void ManageConditional(Quest conditional)
    {
        // do nothing
    }

    public override void Dispose()
    {
        _disposable.Dispose();
        ConditionalBook.Dispose();
        foreach (var quest in ConditionalBook)
        {
            RemovedConditionalHandlers(quest);
        }
    }

    public void HandleInraidQuestPacket(InRaidQuestPacket packet)
    {
        switch (packet.Type)
        {
            case InRaidQuestPacket.InraidQuestType.Finish:
                {
                    FikaGlobals.LogInfo($"Processing {packet.Items.Count} items from quest reward for {Profile.Info.MainProfileNickname}");
                    List<QuestReward> readList = [];
                    foreach (var item in packet.Items)
                    {
                        readList.Add(new()
                        {
                            items = item,
                            _stashId = MongoID.Generate(true),
                            type = ERewardType.Item
                        });
                    }

                    var generatedItems = 0;
                    List<MoveResult> results = [];
                    OperationResult appendResult = default;
                    foreach (var item in readList)
                    {
                        appendResult = item.TryAppendClaimResults(InventoryController, results, out var clonedCount);
                        generatedItems += clonedCount;
                        if (appendResult.Failed)
                        {
                            break;
                        }
                    }
                    if (appendResult.Failed)
                    {
                        results.RollBack();
                        for (var i = 0; i < generatedItems; i++)
                        {
                            InventoryController.RollBack();
                        }
                        return;
                    }

                    method_5(results);
                }
                break;
            case InRaidQuestPacket.InraidQuestType.Handover:
                {
                    FikaGlobals.LogInfo($"Discarding {packet.ItemIdsToRemove.Count} items from {Profile.Info.MainProfileNickname}");
                    List<Item> itemsToRemove = [];
                    var gameWorld = Singleton<GameWorld>.Instance;
                    foreach (string itemId in packet.ItemIdsToRemove)
                    {
                        var result = gameWorld.FindItemById(itemId);
                        if (result.Failed)
                        {
                            FikaGlobals.LogError($"Could not find itemId {itemId}: {result.Error}");
                            continue;
                        }
                        itemsToRemove.Add(result.Value);
                    }

                    List<OperationResult<DiscardResult>> list = [];
                    OperationResult<DiscardResult> discardResult = default;
                    for (var i = 0; i < itemsToRemove.Count; i++)
                    {
                        discardResult = ItemManipulator.Discard(itemsToRemove[i], InventoryController, false);
                        if (discardResult.Failed)
                        {
                            break;
                        }
                        list.Add(discardResult);
                    }

                    if (discardResult.Failed)
                    {
                        list.RollBack();
                        FikaGlobals.LogError($"Could not discard items: {discardResult.Error.Localized()}");
                    }
                }
                break;
        }
    }
}

public sealed class QuestInformation
{
    public MongoID ConditionId { get; set; }

    public int Value { get; set; }
}

public sealed class ZoneDropInformation(MongoID? itemId, string zoneId)
{
    public MongoID? ItemId { get; set; } = itemId;

    public string ZoneId { get; set; } = zoneId;
}