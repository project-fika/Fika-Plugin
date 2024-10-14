using EFT.Communications;
using Fika.Core.Utils;
using Newtonsoft.Json;

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
				return string.Format(LocaleUtils.UI_NOTIFICATION_STARTED_RAID.Localized(),
					ColorUtils.ColorizeText(Colors.GREEN, Nickname.StartsWith("dedicated_") ? "Dedicated Client" : Nickname),
					ColorUtils.ColorizeText(Colors.BLUE, Location.Localized()));
			}
		}

		[JsonProperty("nickname")]
		public string Nickname;
		[JsonProperty("location")]
		public string Location;
	}
}
