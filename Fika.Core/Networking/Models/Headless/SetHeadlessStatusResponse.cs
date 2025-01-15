using System.Runtime.Serialization;

namespace Fika.Core.Networking.Http
{
    [DataContract]
    public struct SetHeadlessStatusResponse
    {
        [DataMember(Name = "sessionId")]
        public string SessionId { get; set; }

        [DataMember(Name = "status")]
        public EHeadlessStatus Status { get; set; }
    }
}
