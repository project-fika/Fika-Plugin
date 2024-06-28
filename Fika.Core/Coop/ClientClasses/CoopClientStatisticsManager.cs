using EFT;
using EFT.HealthSystem;

namespace Fika.Core.Coop.ClientClasses
{
    public sealed class CoopClientStatisticsManager(Profile profile) : LocationStatisticsCollectorAbstractClass()
    {
        public Profile Profile = profile;

        public new void Init(Profile profile, IHealthController healthController)
        {
            Profile_0 = Profile;
            IHealthController_0 = healthController;
        }

        public override void ShowStatNotification(LocalizationKey localizationKey1, LocalizationKey localizationKey2, int value)
        {
            if (value > 0)
            {
                NotificationManagerClass.DisplayNotification(new StatNotificationClass(localizationKey1, localizationKey2, value));
            }
        }
    }
}
