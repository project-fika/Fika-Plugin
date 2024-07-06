using System.Runtime.Serialization;

namespace Fika.Core.Networking.Http.Models
{
    [DataContract]
    public struct GetHostResponse
    {
        [DataMember(Name = "ips")]
        public string[] Ips;

        [DataMember(Name = "port")]
        public int Port;

        [DataMember(Name = "natPunch")]
        public bool NatPunch;

        public GetHostResponse(string[] ips, int port, bool natPunch)
        {
            Ips = ips;
            Port = port;
            NatPunch = natPunch;
        }
    }
}