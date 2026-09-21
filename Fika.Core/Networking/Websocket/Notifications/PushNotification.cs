using EFT.Communications;
using System.Text.Json.Serialization;
using System;
using Il2CppInterop.Runtime.Injection;
using Fika.Core.Main.Utils;

namespace Fika.Core.Networking.Websocket.Notifications;

public class PushNotification : Notification
{
    public PushNotification(IntPtr pointer) : base(pointer)
    {
    }

    public PushNotification() : base(Il2CppInjection.Allocate<PushNotification>())
    {
        ClassInjector.DerivedConstructorBody(this);
        ClassInjector.InvokeBaseConstructor<Notification>(this);
    }

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

    [JsonPropertyName("notificationIcon")]
    public ENotificationIconType NotificationIcon;

    [JsonPropertyName("notification")]
    public string Notification;
}
