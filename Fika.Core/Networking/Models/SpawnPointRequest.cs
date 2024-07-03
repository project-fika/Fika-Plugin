using Fika.Core.Coop.Components;
using System.Runtime.Serialization;

namespace Fika.Core.Networking.Http.Models
{
    [DataContract]
    public struct SpawnPointRequest
    {
        [DataMember(Name = "serverId")]
        public string ServerId;
        
        [DataMember(Name = "groupId")]
        public string GroupId;

        public SpawnPointRequest(string groupId = "")
        {
            ServerId = CoopHandler.GetServerId();
            GroupId = groupId;
        }
    }
}