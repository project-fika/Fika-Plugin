using System.Runtime.Serialization;
using Fika.Core.Main.Components;
using System.Text.Json.Serialization;

namespace Fika.Core.Networking.Models;

[DataContract]
public struct SetHostRequest
{
    [JsonPropertyName("serverId")]
    public string ServerId;

    [JsonPropertyName("ips")]
    public string[] Ips;

    [JsonPropertyName("port")]
    public ushort Port;

    [JsonPropertyName("natPunch")]
    public bool NatPunch;

    [JsonPropertyName("useFikaNatPunchServer")]
    public bool UseFikaNatPunchServer;

    [JsonPropertyName("isHeadless")]
    public bool IsHeadless;

    public SetHostRequest(string[] ips, ushort port, bool natPunch, bool useFikaNatPunchServer, bool isHeadless)
    {
        ServerId = CoopHandler.GetServerId();
        Ips = ips;
        Port = port;
        NatPunch = natPunch;
        UseFikaNatPunchServer = useFikaNatPunchServer;
        IsHeadless = isHeadless;
    }
}