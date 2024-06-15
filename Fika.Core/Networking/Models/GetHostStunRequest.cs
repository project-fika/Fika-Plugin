using Fika.Core.Coop.Components;
using SPT.Common.Http;
using System.Runtime.Serialization;

namespace Fika.Core.Networking.Http.Models
{
    [DataContract]
    public struct GetHostStunRequest
    {
        [DataMember(Name = "requestType")]
        public string RequestType;

        [DataMember(Name = "sessionId")]
        public string SessionId;

        [DataMember(Name = "serverId")]
        public string ServerId;

        [DataMember(Name = "stunIp")]
        public string StunIp;

        [DataMember(Name = "stunPort")]
        public int StunPort;

        public GetHostStunRequest(string serverId, string sessionId, string stunIp, int stunPort)
        {
            RequestType = GetType().Name;
            SessionId = sessionId;
            ServerId = serverId;
            StunIp = stunIp;
            StunPort = stunPort;
        }
    }
}