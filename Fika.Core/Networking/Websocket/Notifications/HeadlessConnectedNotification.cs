using EFT;
using EFT.Communications;
using Fika.Core.Main.Utils;
using Fika.Core.UI;
using System.Text.Json.Serialization;
using System;
using Il2CppInterop.Runtime.Injection;

namespace Fika.Core.Networking.Websocket.Notifications;

public class HeadlessConnectedNotification : Notification
{
    public HeadlessConnectedNotification(IntPtr pointer) : base(pointer)
    {
    }

    public HeadlessConnectedNotification() : base(Il2CppInjection.Allocate<HeadlessConnectedNotification>())
    {
        ClassInjector.DerivedConstructorBody(this);
        ClassInjector.InvokeBaseConstructor<Notification>(this);
    }

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

    [JsonPropertyName("name")]
    public string Name { get; set; }
}
