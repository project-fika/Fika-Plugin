using Fika.Core.Main.Components;
using System.Runtime.Serialization;

namespace Fika.Core.Networking.Models;

[DataContract]
public struct PlayerSpawnRequest
{
    [DataMember(Name = "serverId")]
    public string ServerId;

    [DataMember(Name = "profileId")]
    public string ProfileId;

    [DataMember(Name = "groupId")]
    public string GroupId;

    public PlayerSpawnRequest(string profileId, string groupId)
    {
        ServerId = CoopHandler.GetServerId();
        ProfileId = profileId;
        GroupId = groupId;
    }
}