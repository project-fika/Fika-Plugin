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
    public class ObservedQuestController : LocalQuestControllerClass
    {
        private IPlayerSearchController searchController;

        public ObservedQuestController(Profile profile, InventoryController inventoryController,
            IPlayerSearchController searchController, IQuestActions session) : base(profile, inventoryController, session)
        {
            this.searchController = searchController;
        }

        public override QuestControllerAbstractClass<QuestClass> InitConditionsConnectorsManager()
        {
            GClass3718_0 = new GClass3723(Profile, inventoryController_0,
                Quests, Achievements, IBackEndSession);
            return GClass3718_0;
        }

        public override Task<IResult> AcceptQuest(QuestClass quest, bool runNetworkTransaction)
        {
            SetConditionalStatus(quest, EQuestStatus.Started);
            return SuccessfulResult.Task;
        }

        public override async Task<GStruct455<GStruct397<QuestClass>>> FinishQuest(QuestClass quest, bool runNetworkTransaction)
        {
            method_6(quest);
            if (quest.QuestStatus != EQuestStatus.AvailableForFinish)
            {
                SetConditionalStatus(quest, EQuestStatus.AvailableForFinish);
            }
            GStruct455<GStruct397<QuestClass>> result = InteractionsHandlerClass.FinishConditional(quest, inventoryController_0,
                this, false);
            if (result.Failed)
            {
                FikaGlobals.LogError(result.Error.Localized());
                return result;
            }

            IEnumerator<QuestClass> enumerator = Quests.GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    GClass3702.Class3440 checker = new()
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
                    searchController.SetItemAsKnown(currentReward.Item, false);
                }
            }
            finally
            {
                rewardsEnumerator?.Dispose();
            }

            await method_5(result.Value.Rewards);
            return result;
        }

        public override Task<IResult> HandoverItem(QuestClass quest, ConditionItem condition, Item[] items, bool runNetworkTransaction)
        {
            GStruct455<GClass3226> gstruct = InteractionsHandlerClass.HandoverQuest(quest, items, condition, this.inventoryController_0, this, false);
            if (gstruct.Failed)
            {
                FikaGlobals.LogError(gstruct.Error.Localized());
                return new FailedResult("Failed to handover item", 0).Task;
            }
            GClass1362[] array = ConditionHandoverItem.ConvertToHandoverItems(items);
            TaskConditionCounterClass taskConditionCounter = Profile.GetTaskConditionCounter(quest, condition.id);
            taskConditionCounter.Value += array.Sum(GClass3702.Class3439.class3439_0.method_0);
            quest.CheckForStatusChange(false, true);
            quest.ProgressCheckers[condition].CallConditionChanged();
            return SuccessfulResult.Task;
        }

        public override void SetConditionalStatus(QuestClass quest, EQuestStatus status)
        {
            quest.TransitionStatus(status, false);
            method_0(quest.Rewards, status);
        }

        public override void Init()
        {
            ConditionalBook = CreateConditionalList();
            ConditionalBook.LoadAll();
        }

        public override void Run()
        {
            // Do nothing
        }

        public override void Dispose()
        {
            compositeDisposableClass?.Dispose();
        }
    }
}
