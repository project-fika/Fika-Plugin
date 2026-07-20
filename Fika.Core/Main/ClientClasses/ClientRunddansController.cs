using JsonType;
using System.Threading;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Weather;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.ClientClasses;

public class ClientRunddansController : NetworkRunddansController
{
    public ClientRunddansController(GlobalConfiguration.RunddansGlobalSettings settings, LocationSettings.Location location) : base(settings, location)
    {
        HandleWeather(settings.ApplyFrozenEveryMS, _cts.Token)
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
            if (gameTimer.PastTime.TotalSeconds >= (double)settings.initialFrozenDelaySec)
            {
                var myPlayer = GamePlayerOwner.MyPlayer;
                if (myPlayer != null && !myPlayer.IsAI
                    && !myPlayer.AIData.IsInside && !CheckBonfires(myPlayer)
                    && !(WeatherController.Instance == null) && WeatherController.Instance.WeatherCurve.Rain
                    >= settings.rainForFrozen &&
                    (DateTimeExtensions.UtcNow - myPlayer.AIData.DrinkTimestamp).TotalSeconds >= (double)settings.drunkImmunitySec)
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
        if (!TryGetConsumableItem(player, out var item))
        {
            FikaGlobals.LogError($"Could not find repair item on {player.Profile.Info.MainProfileNickname}");
            return;
        }

        if (!TryRemoveConsumableItem(item))
        {
            FikaGlobals.LogError($"Remove consumable error on {player.Profile.Info.MainProfileNickname}");
        }
    }
}
