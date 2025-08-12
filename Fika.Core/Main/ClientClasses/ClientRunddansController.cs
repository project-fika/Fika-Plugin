using Comfort.Common;
using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using EFT.Weather;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using System.Threading;
using System.Threading.Tasks;

namespace Fika.Core.Main.ClientClasses;

public class ClientRunddansController : GClass2118
{
    public ClientRunddansController(BackendConfigSettingsClass.GClass1583 settings, LocationSettingsClass.Location location) : base(settings, location)
    {
        HandleWeather(settings.ApplyFrozenEveryMS, CancellationTokenSource_0.Token).HandleExceptions();
    }

    public async Task HandleWeather(int delay, CancellationToken token)
    {
        GameTimerClass gameTimer = Singleton<AbstractGame>.Instance.GameTimer;
        for (; ; )
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
            if (gameTimer.PastTime.TotalSeconds >= (double)Gclass1583_0.initialFrozenDelaySec)
            {
                Player myPlayer = GamePlayerOwner.MyPlayer;
                if (myPlayer != null && !myPlayer.IsAI
                    && !myPlayer.AIData.IsInside && !method_0(myPlayer)
                    && !(WeatherController.Instance == null) && WeatherController.Instance.WeatherCurve.Rain
                    >= Gclass1583_0.rainForFrozen &&
                    (EFTDateTimeClass.UtcNow - myPlayer.AIData.DrinkTimestamp).TotalSeconds >= (double)Gclass1583_0.drunkImmunitySec)
                {
                    ActiveHealthController activeHealthController = myPlayer.ActiveHealthController;
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
        if (!method_5(player, out Item item))
        {
            FikaGlobals.LogError($"Could not find repair item on {player.Profile.Info.MainProfileNickname}");
            return;
        }
        if (!method_10(item))
        {
            FikaGlobals.LogError($"Remove consumable error on {player.Profile.Info.MainProfileNickname}");
            return;
        }
    }
}
