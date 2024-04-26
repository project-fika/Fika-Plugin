using Fika.Core.Coop.Components;
using System.Runtime.Serialization;

namespace Fika.Core.Coop.Models
{
    [DataContract]
    public struct GetHostRequest
    {
        [DataMember(Name = "serverId")]
        public string ServerId;

        public GetHostRequest()
        {
            ServerId = CoopHandler.GetServerId();
        }
    }
}