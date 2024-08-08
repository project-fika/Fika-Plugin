using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.UI;
using EFT.UI.Matchmaker;
using Fika.Core.Coop.Utils;
using Fika.Core.UI.Custom;
using Fika.Core.UI.Patches.MatchmakerAcceptScreen;
using Newtonsoft.Json.Linq;
using SPT.Common.Http;
using System;
using UnityEngine;
using WebSocketSharp;

namespace Fika.Core.Networking.Websocket
{
	public class DedicatedRaidWebSocketClient
	{
		private static ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource("Fika.DedicatedWebSocket");

		public string Host { get; set; }
		public string Url { get; set; }
		public string SessionId { get; set; }
		public bool Connected
		{
			get
			{
				return _webSocket.ReadyState == WebSocketState.Open;
			}
		}

		private WebSocket _webSocket;

		public DedicatedRaidWebSocketClient()
		{
			Host = RequestHandler.Host.Replace("http", "ws");
			SessionId = RequestHandler.SessionId;
			Url = $"{Host}/fika/dedicatedraidservice/{SessionId}?";

			_webSocket = new WebSocket(Url)
			{
				WaitTime = TimeSpan.FromMinutes(1),
				EmitOnPing = true
			};

			_webSocket.OnOpen += WebSocket_OnOpen;
			_webSocket.OnError += WebSocket_OnError;
			_webSocket.OnMessage += (sender, args) =>
			{
				// Run the OnMessage event on main thread
				MainThreadDispatcher.RunOnMainThread(() =>
				{
					WebSocket_OnMessage(sender, args);
				});
			};
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

		private void WebSocket_OnOpen(object sender, EventArgs e)
		{
			logger.LogInfo("Connected to FikaDedicatedRaidWebSocket as client");
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

			// TODO: Convert to bytes and use an enum...
			string type = jsonObject.Value<string>("type");

			switch (type)
			{
				case "fikaDedicatedJoinMatch":
					string matchId = jsonObject.Value<string>("matchId");

					GameObject matchmakerObject = MatchmakerAcceptScreen_Show_Patch.MatchmakerObject;
					MatchMakerAcceptScreen matchMakerAcceptScreen = FikaBackendUtils.MatchMakerAcceptScreenInstance;

					if (!string.IsNullOrEmpty(matchId))
					{
						TarkovApplication tarkovApplication = (TarkovApplication)Singleton<ClientApplication<ISession>>.Instance;

						tarkovApplication.StartCoroutine(MatchMakerUIScript.JoinMatch(tarkovApplication.Session.Profile.Id, matchId, null, () =>
						{
							// Hide matchmaker UI
							matchmakerObject.SetActive(false);

							// Matchmaker next screen (accept)
							matchMakerAcceptScreen.method_22();
						}, false));
					}
					else
					{
						PreloaderUI.Instance.ShowErrorScreen("Fika Dedicated Error", "Received fikaJoinMatch WS event but there was no matchId");
						matchmakerObject.SetActive(true);
					}

					FikaPlugin.DedicatedRaidWebSocket.Close();

					break;
			}
		}
	}
}
