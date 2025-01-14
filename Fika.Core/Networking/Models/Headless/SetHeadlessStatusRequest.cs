using System.Runtime.Serialization;

namespace Fika.Core.Networking.Http
{
    [DataContract]
    public struct SetHeadlessStatusRequest
    {
        [DataMember(Name = "sessionId")]
        public string SessionId { get; set; }

        [DataMember(Name = "status")]
        public HeadlessStatus Status { get; set; }

        public SetHeadlessStatusRequest(string sessionId, HeadlessStatus status)
        {
            SessionId = sessionId;
            Status = status;
        }
    }
}
