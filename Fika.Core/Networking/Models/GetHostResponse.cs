using System.Runtime.Serialization;

namespace Fika.Core.Networking.Models;

[DataContract]
public struct GetHostResponse
{
    [DataMember(Name = "ips")]
    public string[] Ips;

    [DataMember(Name = "port")]
    public int Port;

    [DataMember(Name = "natPunch")]
    public bool NatPunch;

    [DataMember(Name = "isHeadless")]
    public bool IsHeadless;

    public GetHostResponse(string[] ips, int port, bool natPunch, bool isHeadless)
    {
        Ips = ips;
        Port = port;
        NatPunch = natPunch;
        IsHeadless = isHeadless;
    }

    public override readonly string ToString()
    {
        string ips = string.Join("; ", Ips);
        return $"HostResponse Data: IPs: {ips}, Port: {Port}, NatPunch: {NatPunch}, IsHeadless: {IsHeadless}";
    }
}