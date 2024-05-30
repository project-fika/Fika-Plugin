using Fika.Core.Coop.Components;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Fika.Core.Networking.Http.Models
{
    [DataContract]
    public struct SetHostRequest
    {
        [DataMember(Name = "serverId")]
        public string ServerId;

        [DataMember(Name = "ips")]
        public string[] Ips;

        [DataMember(Name = "port")]
        public int Port;

        public SetHostRequest(string[] ips, int port)
        {
            ServerId = CoopHandler.GetServerId();
            Ips = ips;
            Port = port;
        }
    }
}