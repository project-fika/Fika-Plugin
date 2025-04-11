using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.Quests;
using HarmonyLib;
using System.Threading.Tasks;

namespace Fika.Core.Coop.ObservedClasses
{
    public class ObservedQuestController(Profile profile, InventoryController inventoryController, IQuestActions session)
        : LocalQuestControllerClass(profile, inventoryController, session)
    {
        public override QuestControllerAbstractClass<QuestClass> InitConditionsConnectorsManager()
        {
            return null;
        }

        public override Task<IResult> AcceptQuest(QuestClass quest, bool runNetworkTransaction)
        {
            return SuccessfulResult.Task;
        }

        public override Task<GStruct455<GStruct397<QuestClass>>> FinishQuest(QuestClass quest, bool runNetworkTransaction)
        {
            return default;
        }

        public override Task<IResult> HandoverItem(QuestClass quest, ConditionItem condition, Item[] items, bool runNetworkTransaction)
        {
            return SuccessfulResult.Task;
        }

        public override void SetConditionalStatus(QuestClass quest, EQuestStatus status)
        {
            // Do nothing
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
