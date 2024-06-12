using EFT;
using EFT.Quests;
using EFT.UI;
using Fika.Core.Coop.Players;
using Fika.Core.Networking.Packets;
using System;
using System.Collections.Generic;

namespace Fika.Core.Coop.ClientClasses
{
    public sealed class CoopSharedQuestController(Profile profile, InventoryControllerClass inventoryController,
        GInterface161 session, CoopPlayer player, bool fromServer = true) : GClass3228(profile, inventoryController, session, fromServer)
    {
        private readonly CoopPlayer player = player;
        private readonly List<string> lastFromNetwork = [];

        public override void OnConditionValueChanged(IConditionCounter conditional, EQuestStatus status, Condition condition, bool notify = true)
        {
            base.OnConditionValueChanged(conditional, status, condition, notify);
            if (lastFromNetwork.Contains(condition.id))
            {
                lastFromNetwork.Remove(condition.id);
                return;
            }
            SendQuestPacket(conditional, condition.id);
        }

        public void AddNetworkId(string id)
        {
            if (!lastFromNetwork.Contains(id))
            {
                lastFromNetwork.Add(id);
            }            
        }

        private void SendQuestPacket(IConditionCounter conditional, string conditionId)
        {
            if (conditional is GClass1258 quest)
            {
                GClass3241 counter = quest.ConditionCountersManager.GetCounter(conditionId);
                if (counter != null)
                {
                    QuestConditionPacket packet = new(player.Profile.Info.MainProfileNickname, counter.Id, counter.SourceId);
                    player.PacketSender.SendQuestPacket(ref packet); 
                }
            }
        }

        internal void ReceiveQuestPacket(ref QuestConditionPacket packet)
        {
            AddNetworkId(packet.Id);
            foreach (GClass1258 quest in Quests)
            {
                if (quest.Id == packet.SourceId && quest.QuestStatus == EQuestStatus.Started)
                {
                    GClass3241 counter = quest.ConditionCountersManager.GetCounter(packet.Id);
                    if (counter != null)
                    {
                        counter.Value++;
                        NotificationManagerClass.DisplayMessageNotification($"Received shared quest progression from {packet.Nickname}",
                            iconType: EFT.Communications.ENotificationIconType.Quest);
                    }
                }
            }
        }
    }
}
