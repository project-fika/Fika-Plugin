using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;

namespace Fika.Core.Coop.HostClasses
{
    public class HostRunddansController : LocalGameRunddansControllerClass
    {
        public HostRunddansController(BackendConfigSettingsClass.GClass1582 settings, LocationSettingsClass.Location location) : base(settings, location)
        {

        }

        public override void InteractWithEventObject(Player player, InteractPacketStruct packet)
        {
            if (!IsValid(player, out LocalGameTransitControllerClass gclass, out TransitDataClass transitDataClass)
                || !transitDataClass.events)
            {
                return;
            }
            if (!Objects.TryGetValue(packet.objectId, out EventObject eventObject))
            {
                FikaGlobals.LogError($"EventObject with id {packet.objectId} not found)");
                return;
            }
            EventObject.EInteraction interaction = packet.interaction;
            if (interaction != EventObject.EInteraction.Run)
            {
                if (interaction != EventObject.EInteraction.Repair)
                {
                    return;
                }
            }
            else
            {
                if (!method_5(player, out Item item))
                {
                    NoRequiredItemNotification(player);
                    return;
                }
                if (!method_10(item))
                {
                    FikaGlobals.LogError($"Remove consumable error on {player.Profile.Info.MainProfileNickname}");
                    return;
                }
            }
            method_4(packet.objectId, EventObject.EState.Running);
            gclass.EnablePoints(false);
            gclass.UpdateTimers();
        }

        public override void OnTriggerStateChanged(EventObject.EState state)
        {
            base.OnTriggerStateChanged(state);
            RunddansStateEvent stateEvent = new()
            {
                PlayerId = 0,
                Objects = []
            };
            foreach ((int id, EventObject eventObject) in Objects)
            {
                stateEvent.Objects.Add(id, eventObject.State);
            }
            EventControllerEventPacket packet = new()
            {
                Type = EventControllerEventPacket.EEventType.StateEvent,
                Event = stateEvent
            };
            Singleton<FikaServer>.Instance.SendDataToAll(ref packet, LiteNetLib.DeliveryMethod.ReliableOrdered);
        }

        private void NoRequiredItemNotification(Player player)
        {
            if (player.IsYourPlayer)
            {
                method_13();
                return;
            }

            EventControllerEventPacket packet = new()
            {
                Type = EventControllerEventPacket.EEventType.MessageEvent,
                Event = new RunddansMessagesEvent()
                {
                    PlayerId = player.Id,
                    Type = RunddansMessagesEvent.EType.NoRequiredItem
                }
            };
            Singleton<FikaServer>.Instance.SendDataToAll(ref packet, LiteNetLib.DeliveryMethod.ReliableOrdered);
        }

        private void NotInteractableNotification(Player player)
        {
            if (player.IsYourPlayer)
            {
                method_14();
                return;
            }

            EventControllerEventPacket packet = new()
            {
                Type = EventControllerEventPacket.EEventType.MessageEvent,
                Event = new RunddansMessagesEvent()
                {
                    PlayerId = player.Id,
                    Type = RunddansMessagesEvent.EType.NonInteractive
                }
            };
            Singleton<FikaServer>.Instance.SendDataToAll(ref packet, LiteNetLib.DeliveryMethod.ReliableOrdered);
        }
    }
}
