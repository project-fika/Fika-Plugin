using Comfort.Common;
using EFT;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.Players;
using System;
using Il2CppInterop.Runtime.Injection;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.Components;

public class FikaTimeManager : MonoBehaviour
{
    public FikaTimeManager(IntPtr pointer) : base(pointer)
    {
    }

    public CoopGame CoopGame;
    public GameTimer GameTimer;

    public static FikaTimeManager Create(CoopGame game)
    {
        var timeManager = game.gameObject.AddComponent<FikaTimeManager>();
        timeManager.CoopGame = game;
        timeManager.GameTimer = game.GameTimer;
        return timeManager;
    }

    protected void Update()
    {
        if (CoopGame.Status == GameStatus.Started && GameTimer != null && GameTimer.SessionTime.HasValue && GameTimer.PastTime >= GameTimer.SessionTime.Value)
        {
            CoopGame.ExitStatus = ExitStatus.MissingInAction;
            var fikaPlayer = (FikaPlayer)Singleton<GameWorld>.Instance.MainPlayer;
            CoopGame.Extract(fikaPlayer, null);
            enabled = false;
        }
    }
}
