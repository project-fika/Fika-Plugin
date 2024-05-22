using Comfort.Common;
using EFT;
using EFT.Communications;
using EFT.Quests;
using Fika.Core.Coop.Matchmaker;
using Fika.Core.Networking;
using UnityEngine;

namespace Fika.Core.Coop.ClientClasses
{
    public class CoopClientAchievementController : AchievementControllerClass
    {
        private readonly FikaClient _fikaClient = Singleton<FikaClient>.Instance;

        public CoopClientAchievementController(Profile profile, InventoryControllerClass inventoryController, ISession session, bool fromServer) : base(profile, inventoryController, session, fromServer)
        {
        }

        public override void OnConditionValueChanged(IConditionCounter conditional, EQuestStatus status, Condition condition, bool notify)
        {
            if (MatchmakerAcceptPatches.IsClient)
            {
                // send netId, string for condition, value
                ConditionChangePacket packet = new(_fikaClient.MyPlayer.NetId, condition.id, condition.value);
                _fikaClient.SendData(_fikaClient.DataWriter, ref packet, LiteNetLib.DeliveryMethod.ReliableUnordered);
                NotificationManagerClass.DisplayMessageNotification("Fika Achievement Condition Changed", ENotificationDurationType.Long, ENotificationIconType.Alert, Color.red);
            }

            base.OnConditionValueChanged(conditional, status, condition, notify);
        }
    }
}