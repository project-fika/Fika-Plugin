using EFT;
using EFT.Communications;

namespace Fika.Core.Main.ClientClasses;

public sealed class ClientStatisticsManager : BaseStatisticsManager
{
    public override void ShowStatNotification(LocalizationKey localizationKey1, LocalizationKey localizationKey2, int value)
    {
        if (value > 0)
        {
            NotificationManager.DisplayNotification(new StatisticNotification(localizationKey1, localizationKey2, value));
        }
    }
}
