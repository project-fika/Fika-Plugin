using Comfort.Common;
using EFT;
using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.Players;
using UnityEngine;

namespace Fika.Core.Coop.Components
{
	internal class CoopTimeManager : MonoBehaviour
	{
		public CoopGame CoopGame;
		public GameTimerClass GameTimer;

		public static CoopTimeManager Create(CoopGame game)
		{
			CoopTimeManager timeManager = game.gameObject.AddComponent<CoopTimeManager>();
			timeManager.CoopGame = game;
			timeManager.GameTimer = game.GameTimer;
			return timeManager;
		}

		protected void Update()
		{
			if (CoopGame.Status == GameStatus.Started && GameTimer != null && GameTimer.SessionTime != null && GameTimer.PastTime >= GameTimer.SessionTime)
			{
				CoopGame.ExitStatus = ExitStatus.MissingInAction;
				CoopPlayer coopPlayer = (CoopPlayer)Singleton<GameWorld>.Instance.MainPlayer;
				CoopGame.Extract(coopPlayer, null);
				enabled = false;
			}
		}
	}
}
