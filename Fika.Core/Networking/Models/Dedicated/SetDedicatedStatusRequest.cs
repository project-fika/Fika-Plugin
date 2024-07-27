using System.Runtime.Serialization;

namespace Fika.Core.Networking.Models.Dedicated
{
    [DataContract]
    public struct SetDedicatedStatusRequest
    {
        [DataMember(Name = "sessionId")]
        public string SessionId { get; set; }

        [DataMember(Name = "status")]
        public string Status { get; set; }

        public SetDedicatedStatusRequest(string sessionId, string status)
        {
            SessionId = sessionId;
            Status = status;
        }
    }
}
