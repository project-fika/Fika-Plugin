using EFT;
using EFT.Communications;
using Fika.Core.Main.Utils;
using JsonType;
using System.Text.Json.Serialization;
using static Fika.Core.UI.FikaUIGlobals;
using System;
using Il2CppInterop.Runtime.Injection;

namespace Fika.Core.Networking.Websocket.Notifications;

public class StartRaidNotification : Notification
{
    public StartRaidNotification(IntPtr pointer) : base(pointer)
    {
    }

    public StartRaidNotification() : base(Il2CppInjection.Allocate<StartRaidNotification>())
    {
        ClassInjector.DerivedConstructorBody(this);
        ClassInjector.InvokeBaseConstructor<Notification>(this);
    }

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
            var message = string.Format(LocaleUtils.UI_NOTIFICATION_STARTED_RAID.Localized(),
                ColorizeText(EColor.GREEN, Nickname.StartsWith("headless_") ? "Headless Client" : Nickname),
                ColorizeText(EColor.BLUE, Location.Localized()));

            if (Location is not "laboratory")
            {
                var time = FormattedTime(RaidTime, Location is "factory4_day" or "factory4_night");
                if (!string.IsNullOrEmpty(time))
                {
                    message += $" ({BoldText(ColorizeText(EColor.BLUE, time))})";
                }
            }

            return message;
        }
    }

    [JsonPropertyName("nickname")]
    public string Nickname;

    [JsonPropertyName("location")]
    public string Location;

    [JsonPropertyName("raidTime")]
    public EDateTime RaidTime;
}
