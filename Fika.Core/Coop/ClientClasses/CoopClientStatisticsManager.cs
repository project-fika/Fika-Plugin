using EFT;

namespace Fika.Core.Coop.ClientClasses
{
    public sealed class CoopClientStatisticsManager : LocationStatisticsCollectorAbstractClass
    {
        public override void ShowStatNotification(LocalizationKey localizationKey1, LocalizationKey localizationKey2, int value)
        {
            if (value > 0)
            {
                NotificationManagerClass.DisplayNotification(new StatNotificationClass(localizationKey1, localizationKey2, value));
            }
        }
    }
}
