using System.Runtime.Serialization;

namespace Fika.Core.Networking.Http.Models
{
    [DataContract]
    public struct GetHostRequest
    {
        [DataMember(Name = "serverId")]
        public string ServerId;

        public GetHostRequest(string serverId)
        {
            ServerId = serverId;
        }
    }
}