using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.Quests;
using HarmonyLib;
using System.Threading.Tasks;

namespace Fika.Core.Coop.ObservedClasses
{
	public class ObservedQuestController(Profile profile, InventoryController inventoryController, IQuestActions session, bool fromServer)
		: LocalQuestControllerClass(profile, inventoryController, session, fromServer)
	{
		public override void InitConditionsConnectorsManager()
		{
			// Do nothing
		}

		public override Task<IResult> AcceptQuest(QuestClass quest, bool runNetworkTransaction)
		{
			return SuccessfulResult.Task;
		}

		public override Task<GStruct446<GStruct388<QuestClass>>> FinishQuest(QuestClass quest, bool runNetworkTransaction)
		{
			return default;
		}

		public override Task<IResult> HandoverItem(QuestClass quest, ConditionItem condition, Item[] items, bool runNetworkTransaction)
		{
			return SuccessfulResult.Task;
		}

		public override void SetConditionalStatus(IConditionCounter quest, EQuestStatus status)
		{
			// Do nothing
		}

		public override void Init()
		{
			Quests = new GClass3773(Profile.Id, Profile.Side, Profile.QuestsData, Profile.TaskConditionCounters, Profile.Info.Type, false);
			gclass3772_0 = Quests;
		}

		public override void Run()
		{
			Quests.LoadAll();
		}

		public override void Dispose()
		{
			CompositeDisposableClass compositeDisposableClass = Traverse.Create(this).Field<CompositeDisposableClass>("compositeDisposableClass").Value;
			compositeDisposableClass?.Dispose();
		}
	}
}
