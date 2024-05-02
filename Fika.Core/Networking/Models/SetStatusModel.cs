using System.Runtime.Serialization;
using static Fika.Core.UI.Models.LobbyEntry;

namespace Fika.Core.UI.Models
{
    [DataContract]
    public struct SetStatusModel
    {
        [DataMember(Name = "serverId")]
        public string ServerId;

        [DataMember(Name = "status")]
        public ELobbyStatus Status;

        public SetStatusModel(string serverId, ELobbyStatus status)
        {
            ServerId = serverId;
            Status = status;
        }
    }
}