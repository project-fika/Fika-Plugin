using JsonType;
using System.Threading;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Weather;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using System;
using Il2CppInterop.Runtime.Injection;

namespace Fika.Core.Main.ClientClasses;

public class ClientRunddansController : NetworkRunddansController
{
    public ClientRunddansController(IntPtr pointer) : base(pointer)
    {
    }

    public ClientRunddansController(RunddansGlobalSettings settings, LocationSettings.Location location) : base(Il2CppInjection.Allocate<ClientRunddansController>())
    {
        ClassInjector.DerivedConstructorBody(this);
        ClassInjector.InvokeBaseConstructor<NetworkRunddansController>(this, settings, location);
        HandleWeather(settings.ApplyFrozenEveryMS, _cts)
            .Forget();
    }

    public async Task HandleWeather(int delay, Il2CppSystem.Threading.CancellationTokenSource cancellation)
    {
        var gameTimer = Singleton<AbstractGame>.Instance.GameTimer;
        while (true)
        {
            try
            {
                if (cancellation.IsCancellationRequested)
                {
                    break;
                }
                await Task.Delay(delay);
            }
            catch
            {
                break;
            }
            if (cancellation.IsCancellationRequested)
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
                    (DateTimeExtensions.UtcNow.ToManaged() - myPlayer.AIData.DrinkTimestamp.ToManaged()).TotalSeconds >= (double)settings.drunkImmunitySec)
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
