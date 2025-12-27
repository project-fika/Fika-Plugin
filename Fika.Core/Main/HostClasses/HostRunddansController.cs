using Comfort.Common;
using EFT;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.Communication;

namespace Fika.Core.Main.HostClasses;

public class HostRunddansController(BackendConfigSettingsClass.GClass1748 settings, LocationSettingsClass.Location location)
    : LocalGameRunddansControllerClass(settings, location)
{
    public override void InteractWithEventObject(Player player, InteractPacketStruct packet)
    {
        LocalGameTransitControllerClass transitController = null;
        if (player.IsYourPlayer)
        {
            if (!IsValid(player, out transitController, out var transitDataClass)
                || !transitDataClass.events)
            {
                return;
            }
        }
        if (transitController == null)
        {
            transitController = (FikaHostTransitController)Singleton<GameWorld>.Instance.TransitController;
        }
        if (transitController == null)
        {
            FikaGlobals.LogError("TransitController was null");
            return;
        }
        if (!Objects.TryGetValue(packet.objectId, out var eventObject))
        {
            FikaGlobals.LogError($"EventObject with id {packet.objectId} not found)");
            return;
        }
        var interaction = packet.interaction;
        if (interaction != EventObject.EInteraction.Run)
        {
            if (interaction != EventObject.EInteraction.Repair)
            {
                return;
            }
        }
        else
        {
            if (!method_5(player, out var item))
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
        transitController.EnablePoints(false);
        transitController.UpdateTimers();

        var startPacket = new EventControllerEventPacket
        {
            Type = EventControllerEventPacket.EEventType.StartedEvent
        };
        Singleton<IFikaNetworkManager>.Instance.SendData(ref startPacket, DeliveryMethod.ReliableOrdered);
    }

    public override void OnTriggerStateChanged(EventObject.EState state)
    {
#if DEBUG
        FikaGlobals.LogInfo($"RunddansStateChanged: {state}");
#endif
        base.OnTriggerStateChanged(state);
        RunddansStateEvent stateEvent = new()
        {
            PlayerId = 0,
            Objects = []
        };
        foreach ((var id, var eventObject) in Objects)
        {
            stateEvent.Objects.Add(id, eventObject.State);
        }
        EventControllerEventPacket packet = new()
        {
            Type = EventControllerEventPacket.EEventType.StateEvent,
            Event = stateEvent
        };
        Singleton<IFikaNetworkManager>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered);
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
            Event = new RunddansMessagesEvent
            {
                PlayerId = player.Id,
                Type = RunddansMessagesEvent.EType.NoRequiredItem
            }
        };
        Singleton<IFikaNetworkManager>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered);
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
        Singleton<IFikaNetworkManager>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered);
    }
}
