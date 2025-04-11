using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.Quests;
using Fika.Core.Coop.Utils;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Fika.Core.Coop.ObservedClasses
{
    public class ObservedQuestController(Profile profile, InventoryController inventoryController, IQuestActions session)
        : LocalQuestControllerClass(profile, inventoryController, session)
    {
        public override QuestControllerAbstractClass<QuestClass> InitConditionsConnectorsManager()
        {
            GClass3718_0 = new GClass3723(Profile, inventoryController_0,
                Quests, Achievements, IBackEndSession);
            return GClass3718_0;
        }

        public override async Task<IResult> AcceptQuest(QuestClass quest, bool runNetworkTransaction)
        {
            IResult result;
            if (runNetworkTransaction)
            {
                result = await iQuestActions.QuestAccept(quest.Id, quest is GClass3691);
            }
            else
            {
                result = SuccessfulResult.New;
            }
            IResult result2 = result;
            if (result2.Succeed)
            {
                SetConditionalStatus(quest, EQuestStatus.Started);
            }
            return result2;
        }

        public override async Task<GStruct455<GStruct397<QuestClass>>> FinishQuest(QuestClass quest, bool runNetworkTransaction)
        {
            IResult result;
            if (runNetworkTransaction)
            {
                result = await iQuestActions.QuestComplete(quest.Id, true, quest is GClass3691);
            }
            else
            {
                result = SuccessfulResult.New;
            }
            IResult result2 = result;
            GStruct455<GStruct397<QuestClass>> gstruct;
            if (result2.Failed)
            {
                gstruct = new GClass3854(result2.Error);
            }
            else
            {
                SetConditionalStatus(quest, EQuestStatus.Success);
                using (IEnumerator<QuestClass> enumerator = Quests.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        Class3437 conditionChecker = new()
                        {
                            testQuest = enumerator.Current
                        };
                        if (conditionChecker.testQuest.QuestStatus == EQuestStatus.Started)
                        {
                            if (conditionChecker.testQuest.Template.Conditions.TryGetValue(EQuestStatus.Fail, out GClass3878 gclass))
                            {
                                if (gclass.Any(conditionChecker.method_0))
                                {
                                    SetConditionalStatus(conditionChecker.testQuest, EQuestStatus.Fail);
                                }
                            }
                        }
                    }
                }
                gstruct = default;
            }
            return gstruct;
        }

        public override async Task<IResult> HandoverItem(QuestClass quest, ConditionItem condition, Item[] items, bool runNetworkTransaction)
        {
            GClass1362[] array = ConditionHandoverItem.ConvertToHandoverItems(items);
            IResult result;
            if (runNetworkTransaction)
            {
                result = await iQuestActions.QuestHandover(quest.Id, condition.id, array);
            }
            else
            {
                result = SuccessfulResult.New;
            }
            IResult result2 = result;
            if (result2.Succeed)
            {
                TaskConditionCounterClass taskConditionCounter = Profile.GetTaskConditionCounter(quest, condition.id);
                taskConditionCounter.Value += array.Sum(Class3436.class3436_0.method_0);
                quest.CheckForStatusChange(false, true);
                quest.ProgressCheckers[condition].CallConditionChanged();
            }
            else
            {
                FikaGlobals.LogError($"Failed to handover item, quest: {quest.Id}, condition: {condition.id}, error: {result2.Error}");
            }
            return result2;
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
