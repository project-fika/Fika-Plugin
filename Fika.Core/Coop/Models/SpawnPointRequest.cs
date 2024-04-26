using Fika.Core.Coop.Components;
using System.Runtime.Serialization;

namespace Fika.Core.Coop.Models
{
    [DataContract]
    public struct SpawnPointRequest
    {
        [DataMember(Name = "serverId")]
        public string ServerId;

        public SpawnPointRequest()
        {
            ServerId = CoopHandler.GetServerId();
        }
    }
}