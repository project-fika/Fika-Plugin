using EFT.Communications;
using Fika.Core.Utils;
using Newtonsoft.Json;

namespace Fika.Core.Networking.Websocket.Notifications
{
	public class ReceivedSentItemNotification : NotificationAbstractClass
	{
		public override ENotificationIconType Icon
		{
			get
			{
				return ENotificationIconType.WishlistOther;
			}
		}

		public override string Description
		{
			get
			{
				return string.Format(LocaleUtils.UI_NOTIFICATION_RECEIVED_ITEM.Localized(),
					ColorUtils.ColorizeText(Colors.BLUE, ItemName.Localized()),
					ColorUtils.ColorizeText(Colors.GREEN, Nickname));
			}
		}

		[JsonProperty("nickname")]
		public string Nickname;
		[JsonProperty("targetId")]
		public string TargetId;
		[JsonProperty("itemName")]
		public string ItemName;
	}
}
