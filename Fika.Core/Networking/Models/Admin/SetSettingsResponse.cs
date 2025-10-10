using System.Runtime.Serialization;

namespace Fika.Core.Networking.Models.Admin;

[DataContract]
public struct SetSettingsResponse
{
    [DataMember(Name = "success")]
    public bool Success;
}
