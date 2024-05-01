using Fika.Core.Coop.Components;
using System.Runtime.Serialization;

namespace Fika.Core.Networking.Http.Models
{
    [DataContract]
    public struct PlayerLeftRequest
    {
        [DataMember(Name = "serverId")]
        public string ServerId;

        [DataMember(Name = "profileId")]
        public string ProfileId;

        public PlayerLeftRequest(string profileId)
        {
            ServerId = CoopHandler.GetServerId();
            ProfileId = profileId;
        }
    }
}