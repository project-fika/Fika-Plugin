using EFT.Communications;
using Newtonsoft.Json;

namespace Fika.Core.Networking.Websocket.Notifications
{
	public class PushNotification : NotificationAbstractClass
	{
		//Todo: We can eventually implement more stuff here for users to use such as the notification duration and it's color.
		public override ENotificationIconType Icon
		{
			get
			{
				return NotificationIcon;
			}
		}

		public override string Description
		{
			get
			{
				return Notification;
			}
		}

		[JsonProperty("notificationIcon")]
		public ENotificationIconType NotificationIcon;
		[JsonProperty("notification")]
		public string Notification;
	}
}
