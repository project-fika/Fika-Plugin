using EFT.Communications;
using Fika.Core.Utils;
using Newtonsoft.Json;
using static Fika.Core.UI.FikaUIGlobals;

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
					ColorizeText(EColor.BLUE, ItemName.Localized()),
					ColorizeText(EColor.GREEN, Nickname));
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
