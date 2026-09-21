using EFT.Communications;
using System;
using Il2CppInterop.Runtime.Injection;
using Fika.Core.Main.Utils;
namespace Fika.Core.Networking.Websocket.Notifications;

public class ShutdownClientNotification : Notification
{
    public ShutdownClientNotification(IntPtr pointer) : base(pointer)
    {
    }

    public ShutdownClientNotification() : base(Il2CppInjection.Allocate<ShutdownClientNotification>())
    {
        ClassInjector.DerivedConstructorBody(this);
        ClassInjector.InvokeBaseConstructor<Notification>(this);
    }

    public override string Description
    {
        get
        {
            return "Shutting down client";
        }
    }
}
