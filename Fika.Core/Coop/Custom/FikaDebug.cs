using Comfort.Common;
using Fika.Core.Coop.Components;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using System.Collections.Generic;
using UnityEngine;

namespace Fika.Core.Coop.Custom
{
	internal class FikaDebug : MonoBehaviour
	{
		private CoopHandler coopHandler;
		private Rect windowRect = new(20, 20, 200, 10);
		private int frameCounter = 0;

		private int Ping
		{
			get
			{
				return Singleton<FikaClient>.Instance.Ping;
			}
		}

		private int ServerFPS
		{
			get
			{
				return Singleton<FikaClient>.Instance.ServerFPS;
			}
		}

		private int RTT
		{
			get
			{
				return Singleton<FikaClient>.Instance.NetClient.FirstPeer.RoundTripTime;
			}
		}

		private bool isServer = false;

		private List<CoopPlayer> alivePlayers;
		private List<CoopPlayer> aliveBots;

		protected void Awake()
		{
			if (CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler))
			{
				this.coopHandler = coopHandler;

				if (FikaBackendUtils.IsServer)
				{
					isServer = true;
				}

				alivePlayers = [];
				aliveBots = [];

				enabled = false;

				return;
			}

			FikaPlugin.Instance.FikaLogger.LogError("FikaDebug: CoopHandlera was null!");
			Destroy(this);
		}

		protected void Update()
		{
			frameCounter++;
			if (frameCounter % 300 == 0)
			{
				frameCounter = 0;

				CheckAndAdd();
			}
		}

		private void CheckAndAdd()
		{
			foreach (CoopPlayer player in coopHandler.HumanPlayers)
			{
				if (!alivePlayers.Contains(player) && player.HealthController.IsAlive)
				{
					AddPlayer(player);
				}
			}

			foreach (CoopPlayer player in coopHandler.Players.Values)
			{
				if (!player.gameObject.name.StartsWith("Player_") && !player.IsYourPlayer)
				{
					if (!aliveBots.Contains(player) && player.HealthController.IsAlive)
					{
						AddBot(player);
					}
				}
			}
		}

		protected void OnEnable()
		{
			CheckAndAdd();
		}

		protected void OnDisable()
		{
			foreach (CoopPlayer player in alivePlayers)
			{
				player.OnPlayerDead -= PlayerDied;
			}
			alivePlayers.Clear();

			foreach (CoopPlayer bot in aliveBots)
			{
				bot.OnPlayerDead -= BotDied;
			}
			aliveBots.Clear();
		}

		private void AddPlayer(CoopPlayer player)
		{
			player.OnPlayerDead += PlayerDied;
			alivePlayers.Add(player);
		}

		private void PlayerDied(EFT.Player player, EFT.IPlayer lastAggressor, DamageInfoStruct damageInfo, EBodyPart part)
		{
			player.OnPlayerDead -= PlayerDied;
			alivePlayers.Remove((CoopPlayer)player);
		}

		private void AddBot(CoopPlayer bot)
		{
			bot.OnPlayerDead += BotDied;
			aliveBots.Add(bot);
		}

		private void BotDied(EFT.Player player, EFT.IPlayer lastAggressor, DamageInfoStruct damageInfo, EBodyPart part)
		{
			player.OnPlayerDead -= BotDied;
			aliveBots.Remove((CoopPlayer)player);
		}

		protected void OnGUI()
		{
			GUI.skin.label.alignment = TextAnchor.MiddleLeft;
			GUI.skin.window.alignment = TextAnchor.UpperCenter;

			GUILayout.BeginArea(windowRect);
			GUILayout.BeginVertical();

			windowRect = GUILayout.Window(1, windowRect, DrawWindow, "Fika Debug");

			GUILayout.EndVertical();
			GUILayout.EndArea();
		}

		private void DrawWindow(int windowId)
		{
			GUILayout.Label($"Alive Players: {alivePlayers.Count}");
			GUILayout.Label($"Alive Bots: {aliveBots.Count}");
			if (isServer)
			{
				GUILayout.Label($"Clients: {Singleton<FikaServer>.Instance.NetServer.ConnectedPeersCount}");
			}
			else
			{
				GUILayout.Label($"Ping: {Ping}");
				GUILayout.Label($"RTT: {RTT}");
				GUILayout.Label($"Server FPS: {ServerFPS}");
			}
			GUI.DragWindow();
		}
	}
}
