using BepInEx.Logging;
using Comfort.Common;
using EFT.GlobalEvents;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using LiteNetLib;
using System;
using UnityEngine;

namespace Fika.Core.Coop.Components
{
	internal class CoopHalloweenEventManager : MonoBehaviour
	{
		ManualLogSource Logger;

		private Action summonStartedAction;
		private Action syncStateEvent;
		private Action syncExitsEvent;

		private FikaServer server;

		protected void Awake()
		{
			Logger = BepInEx.Logging.Logger.CreateLogSource("CoopHalloweenEventManager");
		}

		protected void Start()
		{
			Logger.LogInfo("Initializing");

			//Destroy on client as we dont need to listen to these events, and receive them from the host.
			if (FikaBackendUtils.IsClient)
			{
				Logger.LogInfo("Running on client, destroying");

				Destroy(this);

				return;
			}

			server = Singleton<FikaServer>.Instance;

			summonStartedAction = GlobalEventHandlerClass.Instance.SubscribeOnEvent(new Action<HalloweenSummonStartedEvent>(this.OnHalloweenSummonStarted));
			syncStateEvent = GlobalEventHandlerClass.Instance.SubscribeOnEvent(new Action<HalloweenSyncStateEvent>(this.OnHalloweenSyncStateEvent));
			syncExitsEvent = GlobalEventHandlerClass.Instance.SubscribeOnEvent(new Action<HalloweenSyncExitsEvent>(this.OnHalloweenSyncExitsEvent));
		}

		private void OnHalloweenSummonStarted(HalloweenSummonStartedEvent summonStartedEvent)
		{
			Logger.LogDebug("OnHalloweenSummonStarted");

			HalloweenEventPacket packet = new(EHalloweenPacketType.Summon)
			{
				SummonPosition = summonStartedEvent.PointPosition
			};

			server.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
		}

		private void OnHalloweenSyncStateEvent(HalloweenSyncStateEvent syncStateEvent)
		{
			Logger.LogDebug("OnHalloweenSyncStateEvent");

			HalloweenEventPacket packet = new(EHalloweenPacketType.Sync)
			{
				EventState = syncStateEvent.EventState
			};

			server.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
		}

		private void OnHalloweenSyncExitsEvent(HalloweenSyncExitsEvent syncStateEvent)
		{
			Logger.LogDebug("OnHalloweenSyncExitsEvent");

			HalloweenEventPacket packet = new(EHalloweenPacketType.Exit)
			{
				Exit = syncStateEvent.ExitName
			};

			server.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
		}
	}
}
