using Comfort.Common;
using EFT;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.Players;
using UnityEngine;

namespace Fika.Core.Main.Components
{
    public class CoopTimeManager : MonoBehaviour
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
                FikaPlayer coopPlayer = (FikaPlayer)Singleton<GameWorld>.Instance.MainPlayer;
                CoopGame.Extract(coopPlayer, null);
                enabled = false;
            }
        }
    }
}
