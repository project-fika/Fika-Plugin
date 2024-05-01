using Fika.Core.Coop.Components;
using System.Runtime.Serialization;

namespace Fika.Core.Networking.Http.Models
{
    [DataContract]
    public struct SetHostRequest
    {
        [DataMember(Name = "serverId")]
        public string ServerId;

        [DataMember(Name = "ip")]
        public string Ip;

        [DataMember(Name = "port")]
        public int Port;

        public SetHostRequest(string ip, int port)
        {
            ServerId = CoopHandler.GetServerId();
            Ip = ip;
            Port = port;
        }
    }
}