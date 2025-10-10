using Fika.Core.Main.Components;
using System.Runtime.Serialization;

namespace Fika.Core.Networking.Models;

[DataContract]
public struct RaidSettingsRequest
{
    [DataMember(Name = "serverId")]
    public string ServerId;

    public RaidSettingsRequest()
    {
        ServerId = CoopHandler.GetServerId();
    }
}