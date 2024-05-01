using System.Runtime.Serialization;

namespace Fika.Core.Networking.Http.Models
{
    [DataContract]
    public struct GetHostResponse
    {
        [DataMember(Name = "ip")]
        public string Ip;

        [DataMember(Name = "port")]
        public int Port;

        public GetHostResponse(string ip, int port)
        {
            Ip = ip;
            Port = port;
        }
    }
}