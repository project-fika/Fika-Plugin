
namespace Fika.Core.Networking.Websocket;

public enum EFikaHeadlessWSMessageType
{
    KeepAlive = 0,
    HeadlessStartRaid = 1,
    RequesterJoinRaid = 2,
    ShutdownClient = 4
}
