using EFT.Communications;
using Newtonsoft.Json;
using System;

namespace Fika.Core.Networking.Websocket.Notifications
{
	public class PushNotification : NotificationAbstractClass
	{
		//Todo: We can eventually implement more stuff here for users to use such as the notification duration and it's color.
		public override ENotificationIconType Icon
		{
			get
			{
				//Do some exception handling, icon 6 seems to cause an exception, so does going out of the enum's bounds.
				try
				{
					int iconInt = Convert.ToInt32(Icon);

					if (iconInt == 6 || iconInt > 14)
					{
						return ENotificationIconType.Default;
					}
				}
				catch(Exception)
				{
					return ENotificationIconType.Default;
				}

				if (Enum.TryParse(NotificationIcon, out ENotificationIconType icon))
				{
					return icon;
				}
				else
				{
					return ENotificationIconType.Default;
				}
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
		public string NotificationIcon;
		[JsonProperty("notification")]
		public string Notification;
	}
}
