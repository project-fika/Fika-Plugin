using Fika.Core.Coop.Components;
using System.Runtime.Serialization;

namespace Fika.Core.Networking.Http.Models
{
    [DataContract]
    public struct PingRequest
    {
        [DataMember(Name = "serverId")]
        public string ServerId;

        public PingRequest()
        {
            ServerId = CoopHandler.GetServerId();
        }
    }
}