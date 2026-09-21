using System.Runtime.Serialization;
using static Fika.Core.UI.Models.LobbyEntry;
using System.Text.Json.Serialization;

namespace Fika.Core.Networking.Models;

[DataContract]
public struct SetStatusModel
{
    [JsonPropertyName("serverId")]
    public string ServerId;

    [JsonPropertyName("status")]
    public ELobbyStatus Status;

    public SetStatusModel(string serverId, ELobbyStatus status)
    {
        ServerId = serverId;
        Status = status;
    }
}