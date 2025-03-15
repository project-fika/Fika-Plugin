using Fika.Core.Coop.Components;
using System.Runtime.Serialization;

namespace Fika.Core.Networking.Http
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

        [DataMember(Name = "natPunch")]
        public bool NatPunch;

        [DataMember(Name = "isHeadless")]
        public bool IsHeadless;

        public SetHostRequest(string[] ips, int port, bool natPunch, bool isHeadless)
        {
            ServerId = CoopHandler.GetServerId();
            Ips = ips;
            Port = port;
            NatPunch = natPunch;
            IsHeadless = isHeadless;
        }
    }
}