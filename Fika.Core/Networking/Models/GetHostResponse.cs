using System;
using System.Runtime.Serialization;

namespace Fika.Core.Networking.Models;

[DataContract]
public struct GetHostResponse(string[] ips, Guid serverGuid, ushort port, bool natPunch, bool useFikaNatPunchServer, bool isHeadless)
{
    [DataMember(Name = "ips")]
    public string[] IPs = ips;

    [DataMember(Name = "serverGuid")]
    public Guid ServerGuid = serverGuid;

    [DataMember(Name = "port")]
    public ushort Port = port;

    [DataMember(Name = "natPunch")]
    public bool NatPunch = natPunch;

    [DataMember(Name = "useFikaNatPunchServer")]
    public bool UseFikaNatPunchServer = useFikaNatPunchServer;

    [DataMember(Name = "isHeadless")]
    public bool IsHeadless = isHeadless;

    public override readonly string ToString()
    {
        var ips = string.Join("; ", IPs);
        return $"HostResponse Data: IPs: {ips}, Guid: {ServerGuid}, Port: {Port}, NatPunch: {NatPunch}, UseFikaNatPunchServer: {UseFikaNatPunchServer}, IsHeadless: {IsHeadless}";
    }
}