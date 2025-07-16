using BepInEx.Logging;
using Comfort.Common;
using EFT.GlobalEvents;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.World;
using LiteNetLib;
using System;
using UnityEngine;

namespace Fika.Core.Main.Components
{
    internal class CoopHalloweenEventManager : MonoBehaviour
    {
        private ManualLogSource _logger;

        private Action _summonStartedAction;
        private Action _syncStateEvent;
        private Action _syncExitsEvent;

        private FikaServer _server;

        protected void Awake()
        {
            _logger = BepInEx.Logging.Logger.CreateLogSource("CoopHalloweenEventManager");
        }

        protected void Start()
        {
            _logger.LogInfo("Initializing CoopHalloweenEventManager");

            _server = Singleton<FikaServer>.Instance;

            _summonStartedAction = GlobalEventHandlerClass.Instance.SubscribeOnEvent<HalloweenSummonStartedEvent>(OnHalloweenSummonStarted);
            _syncStateEvent = GlobalEventHandlerClass.Instance.SubscribeOnEvent<HalloweenSyncStateEvent>(OnHalloweenSyncStateEvent);
            _syncExitsEvent = GlobalEventHandlerClass.Instance.SubscribeOnEvent<HalloweenSyncExitsEvent>(OnHalloweenSyncExitsEvent);
        }

        private void OnHalloweenSummonStarted(HalloweenSummonStartedEvent summonStartedEvent)
        {
#if DEBUG
            _logger.LogWarning("OnHalloweenSummonStarted");
#endif

            HalloweenEventPacket packet = new()
            {
                PacketType = EHalloweenPacketType.Summon,
                SyncEvent = summonStartedEvent
            };

            _server.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
        }

        private void OnHalloweenSyncStateEvent(HalloweenSyncStateEvent syncStateEvent)
        {
#if DEBUG
            _logger.LogWarning("OnHalloweenSyncStateEvent");
#endif

            HalloweenEventPacket packet = new()
            {
                PacketType = EHalloweenPacketType.Sync,
                SyncEvent = syncStateEvent
            };

            _server.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
        }

        private void OnHalloweenSyncExitsEvent(HalloweenSyncExitsEvent syncStateEvent)
        {
#if DEBUG
            _logger.LogWarning("OnHalloweenSyncExitsEvent");
#endif

            HalloweenEventPacket packet = new()
            {
                PacketType = EHalloweenPacketType.Exit,
                SyncEvent = syncStateEvent
            };

            _server.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
        }
    }
}
