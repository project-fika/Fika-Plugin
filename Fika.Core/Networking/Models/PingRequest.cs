using Fika.Core.Main.Utils;
using System.Runtime.Serialization;

namespace Fika.Core.Networking.Models;

[DataContract]
public struct PingRequest
{
    [DataMember(Name = "serverId")]
    public string ServerId;

    public PingRequest()
    {
        ServerId = FikaBackendUtils.GroupId;
    }
}