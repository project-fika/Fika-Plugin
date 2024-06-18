using Comfort.Common;
using EFT;
using EFT.Quests;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using LiteNetLib;

namespace Fika.Core.Coop.ClientClasses
{
    public class CoopClientQuestController(Profile profile, InventoryControllerClass inventoryController, IQuestActions session, bool fromServer) : GClass3229(profile, inventoryController, session, fromServer)
    {
        private FikaClient fikaClient = Singleton<FikaClient>.Instance;

        public override void OnConditionValueChanged(IConditionCounter conditional, EQuestStatus status, Condition condition, bool notify)
        {
            if (FikaBackendUtils.IsClient)
            {
                ConditionChangePacket packet = new(fikaClient.MyPlayer.NetId, condition.id, condition.value);
                fikaClient.SendData(fikaClient.DataWriter, ref packet, DeliveryMethod.ReliableUnordered);
            }

            base.OnConditionValueChanged(conditional, status, condition, notify);
        }
    }
}