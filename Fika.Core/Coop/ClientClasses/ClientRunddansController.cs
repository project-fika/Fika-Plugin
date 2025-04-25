using Comfort.Common;
using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using EFT.Weather;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using System.Threading;
using System.Threading.Tasks;

namespace Fika.Core.Coop.ClientClasses
{
    public class ClientRunddansController : GClass2053
    {
        public ClientRunddansController(BackendConfigSettingsClass.GClass1510 settings, LocationSettingsClass.Location location) : base(settings, location)
        {
            HandleWeather(settings.ApplyFrozenEveryMS, cancellationTokenSource_0.Token).HandleExceptions();
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
                if (gameTimer.PastTime.TotalSeconds >= (double)gclass1510_0.initialFrozenDelaySec)
                {
                    Player myPlayer = GamePlayerOwner.MyPlayer;
                    if (myPlayer != null && !myPlayer.IsAI
                        && !myPlayer.AIData.IsInside && !method_0(myPlayer)
                        && !(WeatherController.Instance == null) && WeatherController.Instance.WeatherCurve.Rain
                        >= gclass1510_0.rainForFrozen &&
                        (EFTDateTimeClass.UtcNow - myPlayer.AIData.DrinkTimestamp).TotalSeconds >= (double)gclass1510_0.drunkImmunitySec)
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

        public void DestroyItem(CoopPlayer player)
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
}
