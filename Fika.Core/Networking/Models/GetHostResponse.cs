using System;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Fika.Core.Networking.Models;

[DataContract]
public struct GetHostResponse(string[] ips, Guid serverGuid, ushort port, bool natPunch, bool useFikaNatPunchServer, bool isHeadless)
{
    [JsonPropertyName("ips")]
    public string[] IPs = ips;

    [JsonPropertyName("serverGuid")]
    public Guid ServerGuid = serverGuid;

    [JsonPropertyName("port")]
    public ushort Port = port;

    [JsonPropertyName("natPunch")]
    public bool NatPunch = natPunch;

    [JsonPropertyName("useFikaNatPunchServer")]
    public bool UseFikaNatPunchServer = useFikaNatPunchServer;

    [JsonPropertyName("isHeadless")]
    public bool IsHeadless = isHeadless;

    public override readonly string ToString()
    {
        var ips = string.Join("; ", IPs);
        return $"HostResponse Data: IPs: {ips}, Guid: {ServerGuid}, Port: {Port}, NatPunch: {NatPunch}, UseFikaNatPunchServer: {UseFikaNatPunchServer}, IsHeadless: {IsHeadless}";
    }
}