using Fika.Core.Main.Components;
using System.Runtime.Serialization;

namespace Fika.Core.Networking.Models;

[DataContract]
public struct SetHostRequest
{
    [DataMember(Name = "serverId")]
    public string ServerId;

    [DataMember(Name = "ips")]
    public string[] Ips;

    [DataMember(Name = "port")]
    public ushort Port;

    [DataMember(Name = "natPunch")]
    public bool NatPunch;

    [DataMember(Name = "useFikaNatPunchServer")]
    public bool UseFikaNatPunchServer;

    [DataMember(Name = "isHeadless")]
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