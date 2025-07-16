using EFT;

namespace Fika.Core.Main.ClientClasses
{
    public sealed class ClientStatisticsManager : LocationStatisticsCollectorAbstractClass
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
