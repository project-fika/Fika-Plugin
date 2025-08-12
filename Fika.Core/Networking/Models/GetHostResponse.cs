using System;
using System.Runtime.Serialization;

namespace Fika.Core.Networking.Models;

[DataContract]
public struct GetHostResponse(string[] ips, Guid serverGuid, int port, bool natPunch, bool isHeadless)
{
    [DataMember(Name = "ips")]
    public string[] Ips = ips;

    [DataMember(Name = "serverGuid")]
    public Guid ServerGuid = serverGuid;

    [DataMember(Name = "port")]
    public int Port = port;

    [DataMember(Name = "natPunch")]
    public bool NatPunch = natPunch;

    [DataMember(Name = "isHeadless")]
    public bool IsHeadless = isHeadless;

    public override readonly string ToString()
    {
        string ips = string.Join("; ", Ips);
        return $"HostResponse Data: IPs: {ips}, Guid: {ServerGuid}, Port: {Port}, NatPunch: {NatPunch}, IsHeadless: {IsHeadless}";
    }
}