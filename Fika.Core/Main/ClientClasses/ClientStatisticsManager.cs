using EFT;
using EFT.Communications;
using System;
using Il2CppInterop.Runtime.Injection;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.ClientClasses;

public sealed class ClientStatisticsManager : BaseStatisticsManager
{
    public ClientStatisticsManager(IntPtr pointer) : base(pointer)
    {
    }

    public ClientStatisticsManager() : base(Il2CppInjection.Allocate<ClientStatisticsManager>())
    {
        ClassInjector.DerivedConstructorBody(this);
        ClassInjector.InvokeBaseConstructor<BaseStatisticsManager>(this);
    }

    public override void ShowStatNotification(LocalizationKey localizationKey1, LocalizationKey localizationKey2, int value)
    {
        if (value > 0)
        {
            NotificationManager.DisplayNotification(new StatisticNotification(localizationKey1, localizationKey2, value));
        }
    }
}
