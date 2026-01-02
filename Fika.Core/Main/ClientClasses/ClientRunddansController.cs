using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.Weather;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.ClientClasses;

public class ClientRunddansController : GClass2286
{
    public ClientRunddansController(BackendConfigSettingsClass.GClass1748 settings, LocationSettingsClass.Location location) : base(settings, location)
    {
        HandleWeather(settings.ApplyFrozenEveryMS, CancellationTokenSource_0.Token)
            .HandleExceptions();
    }

    public async Task HandleWeather(int delay, CancellationToken token)
    {
        var gameTimer = Singleton<AbstractGame>.Instance.GameTimer;
        while (true)
        {
            try
            {
                token.ThrowIfCancellationRequested();
                await Task.Delay(delay, token);
            }
            catch
            {
                break;
            }
            if (gameTimer.PastTime.TotalSeconds >= (double)Gclass1748_0.initialFrozenDelaySec)
            {
                var myPlayer = GamePlayerOwner.MyPlayer;
                if (myPlayer != null && !myPlayer.IsAI
                    && !myPlayer.AIData.IsInside && !method_0(myPlayer)
                    && !(WeatherController.Instance == null) && WeatherController.Instance.WeatherCurve.Rain
                    >= Gclass1748_0.rainForFrozen &&
                    (EFTDateTimeClass.UtcNow - myPlayer.AIData.DrinkTimestamp).TotalSeconds >= (double)Gclass1748_0.drunkImmunitySec)
                {
                    var activeHealthController = myPlayer.ActiveHealthController;
                    if (activeHealthController != null)
                    {
                        activeHealthController.TryDoExternalBuff("Buffs_Frostbite");
                    }
                }
            }
        }
    }

    public void DestroyItem(FikaPlayer player)
    {
        if (!method_5(player, out var item))
        {
            FikaGlobals.LogError($"Could not find repair item on {player.Profile.Info.MainProfileNickname}");
            return;
        }
        if (!method_10(item))
        {
            FikaGlobals.LogError($"Remove consumable error on {player.Profile.Info.MainProfileNickname}");
        }
    }
}
