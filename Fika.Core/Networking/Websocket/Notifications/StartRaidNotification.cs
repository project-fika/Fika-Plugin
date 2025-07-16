using EFT.Communications;
using Fika.Core.Utils;
using JsonType;
using Newtonsoft.Json;
using static Fika.Core.UI.FikaUIGlobals;

namespace Fika.Core.Networking.Websocket.Notifications
{
    public class StartRaidNotification : NotificationAbstractClass
    {
        public override ENotificationIconType Icon
        {
            get
            {
                return ENotificationIconType.EntryPoint;
            }
        }

        public override string Description
        {
            get
            {
                string message = string.Format(LocaleUtils.UI_NOTIFICATION_STARTED_RAID.Localized(),
                    ColorizeText(EColor.GREEN, Nickname.StartsWith("headless_") ? "Headless Client" : Nickname),
                    ColorizeText(EColor.BLUE, Location.Localized()));

                if (Location is not "laboratory")
                {
                    string time = FormattedTime(RaidTime, Location is "factory4_day" or "factory4_night");
                    if (!string.IsNullOrEmpty(time))
                    {
                        message += $" ({BoldText(ColorizeText(EColor.BLUE, time))})";
                    }
                }

                return message;
            }
        }

        [JsonProperty("nickname")]
        public string Nickname;

        [JsonProperty("location")]
        public string Location;

        [JsonProperty("raidTime")]
        public EDateTime RaidTime;
    }
}
