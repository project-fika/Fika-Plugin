using EFT.Communications;
namespace Fika.Core.Networking.Websocket.Notifications;

public class ShutdownClientNotification : Notification
{
    public override string Description
    {
        get
        {
            return "Shutting down client";
        }
    }
}
