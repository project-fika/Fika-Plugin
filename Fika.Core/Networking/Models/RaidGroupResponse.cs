using System.Runtime.Serialization;

namespace Fika.Core.Networking.Http.Models
{
    [DataContract]
    public struct RaidGroupResponse(string serverId)
    {
        [DataMember(Name = "serverId")]
        public string ServerId = serverId;
    }
}