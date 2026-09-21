using EFT;

namespace Fika.Core.Main.GameMode;

public static class AbstractGameExtensions
{
    public static void SetMatchmakerStatus(this AbstractGame game, string status, float? progress = null)
    {
        if (game is CoopGame coopGame)
        {
            coopGame.SetMatchmakerStatus(status, progress);
        }
    }
}
