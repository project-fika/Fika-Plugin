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

        public GetHostResponse(string[] ips, int port)
        {
            Ips = ips;
            Port = port;
        }
    }
}