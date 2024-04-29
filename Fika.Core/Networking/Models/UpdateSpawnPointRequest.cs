using Fika.Core.Coop.Components;
using System.Runtime.Serialization;

namespace Fika.Core.Networking.Http.Models
{
    [DataContract]
    public struct UpdateSpawnPointRequest
    {
        [DataMember(Name = "serverId")]
        public string ServerId;

        [DataMember(Name = "name")]
        public string Name;

        public UpdateSpawnPointRequest(string name)
        {
            ServerId = CoopHandler.GetServerId();
            Name = name;
        }
    }
}