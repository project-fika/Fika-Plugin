using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.Quests;
using Fika.Core.Coop.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fika.Core.Coop.ClientClasses
{
    public class CoopClientQuestController(Profile profile, InventoryController inventoryController, IPlayerSearchController searchController, IQuestActions session)
        : GClass3702(profile, inventoryController, searchController, session)
    {

        public override async Task<GStruct455<GStruct397<QuestClass>>> FinishQuest(QuestClass quest, bool runNetworkTransaction)
        {
            method_6(quest);
            if (quest.QuestStatus != EQuestStatus.AvailableForFinish)
            {
                SetConditionalStatus(quest, EQuestStatus.AvailableForFinish);
            }
            GStruct455<GStruct397<QuestClass>> result = InteractionsHandlerClass.FinishConditional(quest, inventoryController_0,
                this, runNetworkTransaction);
            if (result.Failed)
            {
                FikaGlobals.LogError(result.Error.Localized());
                return result;
            }

            if (runNetworkTransaction)
            {
                IResult result2 = await inventoryController_0.TryRunNetworkTransaction(result, null);
                if (result2.Failed)
                {
                    FikaGlobals.LogError($"[{quest.Id}] Sync of quest turn in failed: {result2.Error.Localized()}");
                }
            }
            else
            {
                IEnumerator<QuestClass> enumerator = Quests.GetEnumerator();
                try
                {
                    while (enumerator.MoveNext())
                    {
                        Class3440 checker = new()
                        {
                            testQuest = enumerator.Current
                        };
                        if (checker.testQuest.QuestStatus == EQuestStatus.Started)
                        {
                            if (checker.testQuest.Template.Conditions.TryGetValue(EQuestStatus.Fail, out GClass3878 gclass2))
                            {
                                if (gclass2.Any(checker.method_0))
                                {
                                    SetConditionalStatus(checker.testQuest, EQuestStatus.Fail);
                                }
                            }
                        }
                    }
                }
                finally
                {
                    enumerator?.Dispose();
                }

                IEnumerator<GClass3203> rewardsEnumerator = result.Value.Rewards.GetEnumerator();
                try
                {
                    while (rewardsEnumerator.MoveNext())
                    {
                        GClass3203 currentReward = rewardsEnumerator.Current;
                        iplayerSearchController_0.SetItemAsKnown(currentReward.Item, false);
                    }
                }
                finally
                {
                    rewardsEnumerator?.Dispose();
                }

                await method_5(result.Value.Rewards);
            }

            return result;
        }

        public override async Task<IResult> HandoverItem(QuestClass quest, ConditionItem condition, Item[] items, bool runNetworkTransaction)
        {
            GStruct455<GClass3226> result = InteractionsHandlerClass.HandoverQuest(quest, items, condition,
                inventoryController_0, this, runNetworkTransaction);
            if (result.Failed)
            {
                FikaGlobals.LogError(result.Error.Localized());
                return new FailedResult("Failed to handover item", 0);
            }

            if (runNetworkTransaction)
            {
                IResult result2 = await inventoryController_0.TryRunNetworkTransaction(result, null);
                if (result2.Failed)
                {
                    FikaGlobals.LogError($"[{quest.Id}] Sync of handover failed: {result2.Error.Localized()}");
                }
            }
            else
            {
                GClass1362[] array = ConditionHandoverItem.ConvertToHandoverItems(items);
                TaskConditionCounterClass taskConditionCounter = Profile.GetTaskConditionCounter(quest, condition.id);
                taskConditionCounter.Value += array.Sum(Class3439.class3439_0.method_0);
                quest.CheckForStatusChange(false, true);
                quest.ProgressCheckers[condition].CallConditionChanged();                
            }

            return SuccessfulResult.Task.Result;
        }
    }
}
