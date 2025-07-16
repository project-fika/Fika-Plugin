using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.Quests;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Packets.Backend;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fika.Core.Main.ClientClasses
{
    public class ClientQuestController(Profile profile, InventoryController inventoryController, IPlayerSearchController searchController, IQuestActions session, FikaPlayer player)
        : GClass3800(profile, inventoryController, searchController, session)
    {
        protected readonly FikaPlayer _player = player;

        public override async Task<GStruct459<GStruct419<QuestClass>>> FinishQuest(QuestClass quest, bool runNetworkTransaction)
        {
            List<FlatItemsDataClass[]> items = [];
            bool hasRewards = false;
            if (quest.Rewards.TryGetValue(EQuestStatus.Success, out IReadOnlyList<QuestRewardDataClass> list))
            {
                hasRewards = true;
                for (int i = 0; i < list.Count; i++)
                {
                    QuestRewardDataClass item = list[i];
                    if (item.type != ERewardType.Item)
                    {
                        continue;
                    }
                    items.Add(item.items);
                }
            }
            GStruct459<GStruct419<QuestClass>> finishResult = await base.FinishQuest(quest, runNetworkTransaction);
            if (finishResult.Succeeded && hasRewards)
            {
                InraidQuestPacket packet = new()
                {
                    NetId = _player.NetId,
                    Type = InraidQuestPacket.InraidQuestType.Finish,
                    Items = items
                };

                _player.PacketSender.SendPacket(ref packet);
            }
            return finishResult;
        }

        public override async Task<IResult> HandoverItem(QuestClass quest, ConditionItem condition, Item[] items, bool runNetworkTransaction)
        {
            List<MongoID> itemIds = [];
            bool hasNonQuestItem = false;
            for (int i = 0; i < items.Length; i++)
            {
                Item item = items[i];
                if (!item.QuestItem)
                {
                    hasNonQuestItem = true;
                    break;
                }
            }

            if (hasNonQuestItem)
            {
                for (int i = 0; i < items.Length; i++)
                {
                    Item item = items[i];
                    if (item.QuestItem)
                    {
                        continue;
                    }

                    itemIds.Add(item.Id);
                }
            }

            IResult handoverResult = await base.HandoverItem(quest, condition, items, runNetworkTransaction);
            if (handoverResult.Succeed && hasNonQuestItem)
            {

                InraidQuestPacket packet = new()
                {
                    NetId = _player.NetId,
                    Type = InraidQuestPacket.InraidQuestType.Handover,
                    ItemIdsToRemove = itemIds
                };

                _player.PacketSender.SendPacket(ref packet);
            }
            return handoverResult;
        }
    }
}
