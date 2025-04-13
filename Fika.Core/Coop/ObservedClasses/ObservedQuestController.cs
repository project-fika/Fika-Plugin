using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.Quests;
using Fika.Core.Coop.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fika.Core.Coop.ObservedClasses
{
    public class ObservedQuestController(Profile profile, InventoryController inventoryController, IPlayerSearchController searchController, IQuestActions session)
        : GClass3702(profile, inventoryController, searchController, session)
    {
        public override async Task<GStruct455<GStruct397<QuestClass>>> FinishQuest(QuestClass quest, bool runNetworkTransaction)
        {
            GStruct455<GStruct397<QuestClass>> taskResult = default;
            FikaGlobals.LogInfo($"{Profile.Info.MainProfileNickname} is turning in quest {quest.Id}");
            method_6(quest);
            if (!quest.Rewards.TryGetValue(EQuestStatus.Success, out IReadOnlyList<GClass3743> rewards))
            {
                rewards = [];
            }

            int clonedCount = 0;
            List<GClass3203> rewardItems = [];
            GStruct454 result = default;
            for (int i = 0; i < rewards.Count; i++)
            {

                result = rewards[i].TryAppendClaimResults(inventoryController_0, rewardItems, out int count);
                clonedCount += count;
                if (result.Failed)
                {
                    break;
                }
            }
            if (result.Failed)
            {
                rewardItems.RollBack();
                for (int i = 0; i < clonedCount; i++)
                {
                    inventoryController_0.RollBack();
                }

                FikaGlobals.LogError($"{result.Error.Localized()}");
                taskResult = result.Error;
            }

            await method_5(rewardItems);

            return taskResult;
        }

        public override Task<IResult> HandoverItem(QuestClass quest, ConditionItem condition, Item[] items, bool runNetworkTransaction)
        {
            string itemsToTurnIn = string.Join(", ", items.Select(x => x.Id));
            FikaGlobals.LogInfo($"{Profile.Info.MainProfileNickname} handing over items for {quest.Id}, items: {itemsToTurnIn}");

            List<GStruct454> discardResults = [];
            GStruct454 result = default;
            for (int i = 0; i < items.Length; i++)
            {
                result = InteractionsHandlerClass.Discard(items[i], inventoryController_0, false);
                if (result.Failed)
                {
                    break;
                }
                discardResults.Add(result);
            }
            if (result.Failed)
            {
                discardResults.RollBack();
            }
            if (result.Failed)
            {
                FikaGlobals.LogError(result.Error.Localized());
                return new FailedResult(result.Error.Localized(), 0).Task;
            }

            return SuccessfulResult.Task;
        }
    }
}
