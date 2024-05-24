using Comfort.Common;
using EFT;
using EFT.Quests;
using Fika.Core.Coop.Matchmaker;
using Fika.Core.Networking;

namespace Fika.Core.Coop.ClientClasses
{
    public class CoopClientQuestController : GClass3206
    {
        private FikaClient fikaClient = Singleton<FikaClient>.Instance;

        public CoopClientQuestController(Profile profile, InventoryControllerClass inventoryController, ISession session, bool fromServer) : base(profile, inventoryController, session, fromServer)
        {
        }

        public override void OnConditionValueChanged(IConditionCounter conditional, EQuestStatus status, Condition condition, bool notify)
        {
            if (MatchmakerAcceptPatches.IsClient)
            {
                ConditionChangePacket packet = new(fikaClient.MyPlayer.NetId, condition.id, condition.value);
                fikaClient.SendData(fikaClient.DataWriter, ref packet, LiteNetLib.DeliveryMethod.ReliableUnordered);
            }

            base.OnConditionValueChanged(conditional, status, condition, notify);
        }
    }
}