using System;
using EFT;
using EFT.Communications;
using Fika.Core.Main.Utils;
using System.Text.Json.Serialization;
using static Fika.Core.UI.FikaUIGlobals;
using Il2CppInterop.Runtime.Injection;

namespace Fika.Core.Networking.Websocket.Notifications;

public sealed class ReceivedSentItemNotification : Notification
{
    public ReceivedSentItemNotification(IntPtr pointer) : base(pointer)
    {
    }

    public ReceivedSentItemNotification() : base(Il2CppInjection.Allocate<ReceivedSentItemNotification>())
    {
        ClassInjector.DerivedConstructorBody(this);
        ClassInjector.InvokeBaseConstructor<Notification>(this);
    }

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

    [JsonPropertyName("nickname")]
    public string Nickname;

    [JsonPropertyName("targetId")]
    public string TargetId;

    [JsonPropertyName("itemName")]
    public string ItemName;

    [JsonPropertyName("stackCount")]
    public double StackCount;

    [JsonPropertyName("multiple")]
    public bool Multiple;
}
