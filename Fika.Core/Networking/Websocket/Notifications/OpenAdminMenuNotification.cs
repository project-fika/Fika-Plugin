using EFT.Communications;
using System.Text.Json.Serialization;
using System;
using Il2CppInterop.Runtime.Injection;
using Fika.Core.Main.Utils;

namespace Fika.Core.Networking.Websocket.Notifications;

internal class OpenAdminMenuNotification : Notification
{
    public OpenAdminMenuNotification(IntPtr pointer) : base(pointer)
    {
    }

    public OpenAdminMenuNotification() : base(Il2CppInjection.Allocate<OpenAdminMenuNotification>())
    {
        ClassInjector.DerivedConstructorBody(this);
        ClassInjector.InvokeBaseConstructor<Notification>(this);
    }

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

    [JsonPropertyName("success")]
    public bool Success { get; set; }
}
