using BepInEx.Logging;
using Comfort.Common;
using EFT.UI;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking.Websocket.Notifications;
using Newtonsoft.Json.Linq;
using SPT.Common.Http;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using UnityEngine;
using WebSocketSharp;

namespace Fika.Core.Networking.Websocket
{
	public class FikaNotificationManager : MonoBehaviour
	{
		private static readonly ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource("FikaNotificationManager");
		public static FikaNotificationManager Instance;
		public static bool Exists
		{
			get
			{
				return Instance != null;
			}
		}

		public string Host { get; set; }
		public string Url { get; set; }
		public string SessionId { get; set; }
		public bool Reconnecting { get; private set; }
		public bool Connected
		{
			get
			{
				return _webSocket.ReadyState == WebSocketState.Open;
			}
		}

		private WebSocket _webSocket;
		// Add a queue for incoming notifications, so they can be brought to the main thread in a nice way.
		private ConcurrentQueue<NotificationAbstractClass> notificationsQueue = new();

		public void Awake()
		{
			Instance = this;
			Host = RequestHandler.Host.Replace("http", "ws");
			SessionId = RequestHandler.SessionId;
			Url = $"{Host}/fika/notification/";

			_webSocket = new WebSocket(Url)
			{
				WaitTime = TimeSpan.FromMinutes(1),
				EmitOnPing = true
			};

			_webSocket.SetCredentials(SessionId, "", true);

			_webSocket.OnError += WebSocket_OnError;
			_webSocket.OnMessage += WebSocket_OnMessage;
			_webSocket.OnClose += (sender, e) =>
			{
				// Prevent looping the Coroutine over and over.
				if (Reconnecting)
				{
					return;
				}

				StartCoroutine(ReconnectWebSocket());
			};

			Connect();
		}

		public void Update()
		{
			while (notificationsQueue.TryDequeue(out NotificationAbstractClass notification))
			{
				Singleton<PreloaderUI>.Instance.NotifierView.method_3(notification);
			}
		}

		private void WebSocket_OnError(object sender, ErrorEventArgs e)
		{
			logger.LogInfo($"WS error: {e.Message}");
		}

		public void Connect()
		{
			_webSocket.Connect();
		}

		public void Close()
		{
			_webSocket.Close();
		}

		private void WebSocket_OnMessage(object sender, MessageEventArgs e)
		{
			if (e == null)
			{
				return;
			}

			if (string.IsNullOrEmpty(e.Data))
			{
				return;
			}

			JObject jsonObject = JObject.Parse(e.Data);

			if (!jsonObject.ContainsKey("type"))
			{
				return;
			}

			EFikaNotifications type = (EFikaNotifications)Enum.Parse(typeof(EFikaNotifications), jsonObject.Value<string>("type"));

#if DEBUG
			logger.LogDebug($"Received type: {type}");
#endif

			switch (type)
			{
				case EFikaNotifications.StartedRaid:
					StartRaidNotification startRaidNotification = e.Data.ParseJsonTo<StartRaidNotification>([]);

					if (FikaGlobals.IsInRaid())
					{
						return;
					}

					notificationsQueue.Enqueue(startRaidNotification);
					break;
				case EFikaNotifications.SentItem:
					ReceivedSentItemNotification SentItemNotification = e.Data.ParseJsonTo<ReceivedSentItemNotification>([]);

					notificationsQueue.Enqueue(SentItemNotification);
					break;
				case EFikaNotifications.PushNotification:
					PushNotification pushNotification = e.Data.ParseJsonTo<PushNotification>([]);

					notificationsQueue.Enqueue(pushNotification);
					break;
			}
		}

		private IEnumerator ReconnectWebSocket()
		{
			Reconnecting = true;

			while (true)
			{
				if (_webSocket.ReadyState == WebSocketState.Open)
				{
					break;
				}

				// Don't attempt to reconnect if we're still attempting to connect.
				if (_webSocket.ReadyState != WebSocketState.Connecting)
				{
					_webSocket.Connect();
				}
				yield return new WaitForSeconds(10f);
			}

			Reconnecting = false;

			yield return null;
		}

#if DEBUG
		public static void TestNotification(EFikaNotifications type)
		{
			// Ugly ass one-liner, who cares. It's for debug purposes
			string Username = FikaPlugin.DevelopersList.ToList()[new System.Random().Next(FikaPlugin.DevelopersList.Count)].Key;

			switch (type)
			{
				case EFikaNotifications.StartedRaid:
					StartRaidNotification startRaidNotification = new()
					{
						Nickname = Username,
						Location = "Factory"
					};

					Singleton<PreloaderUI>.Instance.NotifierView.method_3(startRaidNotification);
					break;
				case EFikaNotifications.SentItem:
					ReceivedSentItemNotification SentItemNotification = new()
					{
						Nickname = Username,
						ItemName = "LEDX Skin Transilluminator"
					};

					Singleton<PreloaderUI>.Instance.NotifierView.method_3(SentItemNotification);
					break;
				case EFikaNotifications.PushNotification:
					PushNotification PushNotification = new()
					{
						Notification = "Test notification",
						NotificationIcon = EFT.Communications.ENotificationIconType.Note
					};

					Singleton<PreloaderUI>.Instance.NotifierView.method_3(PushNotification);
					break;
			}
		}
#endif
	}
}
