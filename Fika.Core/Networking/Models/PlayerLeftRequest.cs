using System.Runtime.Serialization;
using Fika.Core.Main.Components;

namespace Fika.Core.Networking.Models;

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