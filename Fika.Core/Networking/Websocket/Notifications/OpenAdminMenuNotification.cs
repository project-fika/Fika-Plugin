using EFT.Communications;
using Newtonsoft.Json;

namespace Fika.Core.Networking.Websocket.Notifications
{
    internal class OpenAdminMenuNotification : NotificationAbstractClass
    {
        public override ENotificationIconType Icon
        {
            get
            {
                return ENotificationIconType.Alert;
            }
        }

        public override string Description
        {
            get
            {
                return "This should not be seen";
            }
        }

        [JsonProperty("success")]
        public bool Success { get; set; }
    }
}
