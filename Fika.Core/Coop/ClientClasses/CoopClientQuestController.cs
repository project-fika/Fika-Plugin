using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.Quests;
using Fika.Core.Coop.Players;
using Fika.Core.Networking.Packets.Backend;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fika.Core.Coop.ClientClasses
{
    public class CoopClientQuestController(Profile profile, InventoryController inventoryController, IPlayerSearchController searchController, IQuestActions session, CoopPlayer player)
        : GClass3702(profile, inventoryController, searchController, session)
    {
        protected readonly CoopPlayer player = player;

        public override Task<GStruct455<GStruct397<QuestClass>>> FinishQuest(QuestClass quest, bool runNetworkTransaction)
        {
            List<GClass1319[]> items = [];
            bool hasRewards = false;
            if (quest.Rewards.TryGetValue(EQuestStatus.Success, out IReadOnlyList<GClass3743> list))
            {
                hasRewards = true;
                foreach (GClass3743 item in list)
                {
                    items.Add(item.items);
                }
            }
            Task<GStruct455<GStruct397<QuestClass>>> finishResult = base.FinishQuest(quest, runNetworkTransaction);
            if (finishResult.Result.Succeeded && hasRewards)
            {
                InraidQuestPacket packet = new()
                {
                    NetId = player.NetId,
                    Type = InraidQuestPacket.InraidQuestType.Finish,
                    Items = items
                };

                player.PacketSender.SendPacket(ref packet);
            }
            return finishResult;
        }

        public override Task<IResult> HandoverItem(QuestClass quest, ConditionItem condition, Item[] items, bool runNetworkTransaction)
        {
            List<string> itemIds = [];
            bool hasNonQuestItem = false;
            foreach (Item item in items)
            {
                if (!item.QuestItem)
                {
                    hasNonQuestItem = true;
                    break;
                }
            }

            if (hasNonQuestItem)
            {
                foreach (Item item in items)
                {
                    if (item.QuestItem)
                    {
                        continue;
                    }

                    itemIds.Add(item.Id);
                } 
            }

            Task<IResult> handoverResult = base.HandoverItem(quest, condition, items, runNetworkTransaction);
            if (handoverResult.Result.Succeed && hasNonQuestItem)
            {
                
                InraidQuestPacket packet = new()
                {
                    NetId = player.NetId,
                    Type = InraidQuestPacket.InraidQuestType.Handover,
                    ItemIdsToRemove = itemIds
                };

                player.PacketSender.SendPacket(ref packet);
            }
            return handoverResult;
        }
    }
}
