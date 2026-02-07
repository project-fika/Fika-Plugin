using Comfort.Common;
using EFT;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.Players;

namespace Fika.Core.Main.Components;

public class FikaTimeManager : MonoBehaviour
{
    public CoopGame CoopGame;
    public GameTimerClass GameTimer;

    public static FikaTimeManager Create(CoopGame game)
    {
        var timeManager = game.gameObject.AddComponent<FikaTimeManager>();
        timeManager.CoopGame = game;
        timeManager.GameTimer = game.GameTimer;
        return timeManager;
    }

    protected void Update()
    {
        if (CoopGame.Status == GameStatus.Started && GameTimer != null && GameTimer.SessionTime != null && GameTimer.PastTime >= GameTimer.SessionTime)
        {
            CoopGame.ExitStatus = ExitStatus.MissingInAction;
            var fikaPlayer = (FikaPlayer)Singleton<GameWorld>.Instance.MainPlayer;
            CoopGame.Extract(fikaPlayer, null);
            enabled = false;
        }
    }
}
