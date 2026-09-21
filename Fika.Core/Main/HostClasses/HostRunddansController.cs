using Comfort.Common;
using EFT;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.Communication;
using JsonType;
using System;
using Il2CppInterop.Runtime.Injection;

namespace Fika.Core.Main.HostClasses;

public class HostRunddansController : LocalRunddansController
{
    public HostRunddansController(IntPtr pointer) : base(pointer)
    {
    }

    public HostRunddansController(RunddansGlobalSettings settings, LocationSettings.Location location) : base(Il2CppInjection.Allocate<HostRunddansController>())
    {
        ClassInjector.DerivedConstructorBody(this);
        ClassInjector.InvokeBaseConstructor<LocalRunddansController>(this, settings, location);
    }

    public override void InteractWithEventObject(Player player, InteractWithEventObjectPacket packet)
    {
        if (!IsValid(player, out LocalTransitController transitController, out var transitDataClass)
                || !transitDataClass.events)
        {
            return;
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
            if (!TryGetConsumableItem(player, out var item))
            {
                NoRequiredItemNotification(player);
                return;
            }
            if (!TryRemoveConsumableItem(item))
            {
                FikaGlobals.LogError($"Remove consumable error on {player.Profile.Info.MainProfileNickname}");
                return;
            }
        }
        SetState(packet.objectId, EventObject.EState.Running);
        transitController.EnablePoints(false);
        transitController.UpdateTimers();

        var startPacket = new EventControllerEventPacket
        {
            Type = EventControllerEventPacket.EEventType.StartedEvent
        };
        FikaGlobals.NetworkManager.SendData(ref startPacket, DeliveryMethod.ReliableOrdered);
    }

    public void ObservedInteractWithEventObject(Player player, InteractWithEventObjectPacket packet, NetPeer peer)
    {
        if (FikaBackendUtils.IsHeadless)
        {
            ObservedHeadlessInteractWithEventObject(player, packet, peer);
            return;
        }

#if DEBUG
        FikaGlobals.LogInfo($"Player {player.Profile.Info.MainProfileNickname} interacted with object");
#endif
        if (Singleton<GameWorld>.Instance.TransitController is not FikaHostTransitController transitController)
        {
            FikaGlobals.LogError($"TransitController was of wrong type: {Singleton<GameWorld>.Instance.TransitController.GetType().Name}");
            return;
        }
        if (!Objects.TryGetValue(packet.objectId, out var eventObject))
        {
            FikaGlobals.LogError($"EventObject with id {packet.objectId} not found)");
            return;
        }
        var interaction = packet.interaction;
#if DEBUG
        FikaGlobals.LogWarning($"Current interaction: {interaction}");
#endif
        if (interaction != EventObject.EInteraction.Run)
        {
            if (interaction != EventObject.EInteraction.Repair)
            {
                return;
            }
        }
        else
        {
            if (!TryGetConsumableItem(player, out var item))
            {
                FikaGlobals.LogWarning($"{player.Profile.GetCorrectedNickname()} is missing the required item");
                NoRequiredItemNotification(player);
                return;
            }
            if (!TryRemoveConsumableItem(item))
            {
                FikaGlobals.LogError($"Remove consumable error on {player.Profile.Info.MainProfileNickname}");
                return;
            }
        }
        SetState(packet.objectId, EventObject.EState.Running);
        transitController.EnablePoints(false);
        transitController.UpdateTimers();

        var startPacket = new EventControllerEventPacket
        {
            Type = EventControllerEventPacket.EEventType.StartedEvent
        };
        FikaGlobals.NetworkManager.SendData(ref startPacket, DeliveryMethod.ReliableOrdered);

        var removePacket = new EventControllerEventPacket
        {
            NetId = player.Id,
            Type = EventControllerEventPacket.EEventType.RemoveItem
        };
        FikaGlobals.NetworkManager.SendData(ref removePacket, DeliveryMethod.ReliableOrdered);
    }

    private void ObservedHeadlessInteractWithEventObject(Player player, InteractWithEventObjectPacket packet, NetPeer peer)
    {
#if DEBUG
        FikaGlobals.LogInfo($"Player {player.Profile.Info.MainProfileNickname} interacted with object on headless");
#endif
        if (Singleton<GameWorld>.Instance.TransitController is not EFT.TransitController transitController)
        {
            FikaGlobals.LogError($"TransitController was of wrong type: {Singleton<GameWorld>.Instance.TransitController.GetType().Name}");
            return;
        }
        if (!Objects.TryGetValue(packet.objectId, out var eventObject))
        {
            FikaGlobals.LogError($"EventObject with id {packet.objectId} not found)");
            return;
        }
        var interaction = packet.interaction;
#if DEBUG
        FikaGlobals.LogWarning($"Current interaction: {interaction}");
#endif
        if (interaction != EventObject.EInteraction.Run)
        {
            if (interaction != EventObject.EInteraction.Repair)
            {
                return;
            }
        }
        else
        {
            if (!TryGetConsumableItem(player, out var item))
            {
                FikaGlobals.LogWarning($"{player.Profile.GetCorrectedNickname()} is missing the required item");
                NoRequiredItemNotification(player);
                return;
            }
            if (!TryRemoveConsumableItem(item))
            {
                FikaGlobals.LogError($"Remove consumable error on {player.Profile.Info.MainProfileNickname}");
                return;
            }
        }
        SetState(packet.objectId, EventObject.EState.Running);
        transitController.EnablePoints(false);

        var startPacket = new EventControllerEventPacket
        {
            Type = EventControllerEventPacket.EEventType.StartedEvent
        };
        FikaGlobals.NetworkManager.SendData(ref startPacket, DeliveryMethod.ReliableOrdered);

        var removePacket = new EventControllerEventPacket
        {
            NetId = player.Id,
            Type = EventControllerEventPacket.EEventType.RemoveItem
        };
        FikaGlobals.NetworkManager.SendData(ref removePacket, DeliveryMethod.ReliableOrdered);
    }

    public override void OnTriggerStateChanged(EventObject.EState state)
    {
#if DEBUG
        FikaGlobals.LogInfo($"RunddansStateChanged: {state}");
#endif
        base.OnTriggerStateChanged(state);
        RunddansStateEvent stateEvent = new()
        {
            PlayerRaidId = 0,
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
        FikaGlobals.NetworkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered);
    }

    private void NoRequiredItemNotification(Player player)
    {
        if (player.IsYourPlayer)
        {
            NoRequiredItemNotification();
            return;
        }

        EventControllerEventPacket packet = new()
        {
            Type = EventControllerEventPacket.EEventType.MessageEvent,
            Event = new RunddansMessagesEvent
            {
                PlayerRaidId = player.RaidId,
                Type = RunddansMessagesEvent.EType.NoRequiredItem
            }
        };
        FikaGlobals.NetworkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered);
    }

    private void NotInteractableNotification(Player player)
    {
        if (player.IsYourPlayer)
        {
            NonInteractiveNotification();
            return;
        }

        EventControllerEventPacket packet = new()
        {
            Type = EventControllerEventPacket.EEventType.MessageEvent,
            Event = new RunddansMessagesEvent()
            {
                PlayerRaidId = player.RaidId,
                Type = RunddansMessagesEvent.EType.NonInteractive
            }
        };
        FikaGlobals.NetworkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered);
    }
}
