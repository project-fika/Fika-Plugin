using EFT.Communications;
using Fika.Core.Main.Utils;
using Fika.Core.UI;
using Newtonsoft.Json;

namespace Fika.Core.Networking.Websocket.Notifications;

public class HeadlessConnectedNotification : NotificationAbstractClass
{
    public override ENotificationIconType Icon
    {
        get
        {
            return ENotificationIconType.Friend;
        }
    }

    public override string Description
    {
        get
        {
            return string.Format(LocaleUtils.UI_HEADLESS_CONNECTED.Localized(),
                FikaUIGlobals.ColorizeText(FikaUIGlobals.EColor.BLUE, Name));
        }
    }

    [JsonProperty("name")]
    public string Name { get; set; }
}
