using System.Collections.Generic;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.Quests;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Packets.Backend;

namespace Fika.Core.Main.ClientClasses;

public class ClientQuestController(Profile profile, InventoryController inventoryController, IPlayerSearchController searchController, IQuestActions session, FikaPlayer player)
    : GClass4007(profile, inventoryController, searchController, session)
{
    protected readonly FikaPlayer _player = player;

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
}
