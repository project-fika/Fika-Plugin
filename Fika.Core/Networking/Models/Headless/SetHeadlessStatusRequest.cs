using System.Runtime.Serialization;

namespace Fika.Core.Networking.Http
{
    [DataContract]
    public struct SetHeadlessStatusRequest
    {
        [DataMember(Name = "sessionId")]
        public string SessionId { get; set; }

        [DataMember(Name = "status")]
        public EHeadlessStatus Status { get; set; }

        public SetHeadlessStatusRequest(string sessionId, EHeadlessStatus status)
        {
            SessionId = sessionId;
            Status = status;
        }
    }
}
