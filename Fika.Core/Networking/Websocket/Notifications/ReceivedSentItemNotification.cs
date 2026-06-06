using System;
using EFT.Communications;
using Fika.Core.Main.Utils;
using Newtonsoft.Json;
using static Fika.Core.UI.FikaUIGlobals;

namespace Fika.Core.Networking.Websocket.Notifications;

public sealed class ReceivedSentItemNotification : NotificationAbstractClass
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
            if (Multiple)
            {
                return string.Format(LocaleUtils.UI_NOTIFICATION_RECEIVED_MULTIPLE_ITEMS.Localized(),
                ColorizeText(EColor.GREEN, Nickname));
            }

            if (StackCount > 1d)
            {
                return string.Format(LocaleUtils.UI_NOTIFICATION_RECEIVED_ITEM_STACK.Localized(),
                (int)Math.Round(StackCount, MidpointRounding.AwayFromZero),
                ColorizeText(EColor.BLUE, ItemName.Localized()),
                ColorizeText(EColor.GREEN, Nickname));
            }

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

    [JsonProperty("stackCount")]
    public double StackCount;

    [JsonProperty("multiple")]
    public bool Multiple;
}
