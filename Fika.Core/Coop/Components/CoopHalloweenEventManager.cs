using BepInEx.Logging;
using Comfort.Common;
using EFT.GlobalEvents;
using Fika.Core.Networking;
using LiteNetLib;
using System;
using UnityEngine;

namespace Fika.Core.Coop.Components
{
	internal class CoopHalloweenEventManager : MonoBehaviour
	{
		private ManualLogSource logger;

		private Action summonStartedAction;
		private Action syncStateEvent;
		private Action syncExitsEvent;

		private FikaServer server;

		protected void Awake()
		{
			logger = BepInEx.Logging.Logger.CreateLogSource("CoopHalloweenEventManager");
		}

		protected void Start()
		{
			logger.LogInfo("Initializing CoopHalloweenEventManager");

			server = Singleton<FikaServer>.Instance;

			summonStartedAction = GlobalEventHandlerClass.Instance.SubscribeOnEvent<HalloweenSummonStartedEvent>(OnHalloweenSummonStarted);
			syncStateEvent = GlobalEventHandlerClass.Instance.SubscribeOnEvent<HalloweenSyncStateEvent>(OnHalloweenSyncStateEvent);
			syncExitsEvent = GlobalEventHandlerClass.Instance.SubscribeOnEvent<HalloweenSyncExitsEvent>(OnHalloweenSyncExitsEvent);
		}

		private void OnHalloweenSummonStarted(HalloweenSummonStartedEvent summonStartedEvent)
		{
#if DEBUG
			logger.LogWarning("OnHalloweenSummonStarted");
#endif

			HalloweenEventPacket packet = new()
			{
				PacketType = EHalloweenPacketType.Summon,
				SyncEvent = summonStartedEvent
			};

			server.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
		}

		private void OnHalloweenSyncStateEvent(HalloweenSyncStateEvent syncStateEvent)
		{
#if DEBUG
			logger.LogWarning("OnHalloweenSyncStateEvent");
#endif

			HalloweenEventPacket packet = new()
			{
				PacketType = EHalloweenPacketType.Sync,
				SyncEvent = syncStateEvent
			};

			server.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
		}

		private void OnHalloweenSyncExitsEvent(HalloweenSyncExitsEvent syncStateEvent)
		{
#if DEBUG
			logger.LogWarning("OnHalloweenSyncExitsEvent");
#endif

			HalloweenEventPacket packet = new()
			{
				PacketType = EHalloweenPacketType.Exit,
				SyncEvent = syncStateEvent
			};

			server.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
		}
	}
}
