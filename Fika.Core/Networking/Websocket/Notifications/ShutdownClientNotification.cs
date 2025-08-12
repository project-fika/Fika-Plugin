namespace Fika.Core.Networking.Websocket.Notifications;

public class ShutdownClientNotification : NotificationAbstractClass
{
    public override string Description
    {
        get
        {
            return "Shutting down client";
        }
    }
}
