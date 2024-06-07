using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.Quests;
using Fika.Core.Coop.Matchmaker;
using Fika.Core.Networking;

namespace Fika.Core.Coop.ClientClasses
{
    public class CoopClientQuestController(Profile profile, InventoryControllerClass inventoryController, bool fromServer) : AbstractQuestControllerClass(profile, inventoryController, fromServer)
    {
        private FikaClient fikaClient = Singleton<FikaClient>.Instance;

        public override void OnConditionValueChanged(IConditionCounter conditional, EQuestStatus status, Condition condition, bool notify)
        {
            if (MatchmakerAcceptPatches.IsClient)
            {
                ConditionChangePacket packet = new(fikaClient.MyPlayer.NetId, condition.id, condition.value);
                fikaClient.SendData(fikaClient.DataWriter, ref packet, LiteNetLib.DeliveryMethod.ReliableUnordered);
            }

            base.OnConditionValueChanged(conditional, status, condition, notify);
        }

        public override Task<IResult> AcceptQuest(GClass1258 quest, bool runNetworkTransaction)
        {
            return Task.FromResult<IResult>(null);
        }

        public override Task<GStruct415<GStruct374<GClass1258>>> FinishQuest(GClass1258 quest, bool runNetworkTransaction)
        {
            return Task.FromResult<GStruct415<GStruct374<GClass1258>>>(null);
        }

        public override Task<IResult> HandoverItem(GClass1258 quest, ConditionItem condition, Item[] items, bool runNetworkTransaction)
        {
            return Task.FromResult<IResult>(null);
        }

        public override void InitConditionsConnectorsManager()
        {
            // do nothing
        }

        public override void Run()
        {
            // do nothing
        }

        public override void SetConditionalStatus(IConditionCounter conditional, EQuestStatus status)
        {
            // do nothing
        }

        public override void TryNotifyConditionalStatusChanged(GClass1258 conditional)
        {
            // do nothing
        }

        public override void TryNotifyConditionChanged(GClass1258 achievement)
        {
            // do nothing
        }

        public override void TryToInstantComplete(IConditionCounter conditional)
        {
            // do nothing
        }
    }
}