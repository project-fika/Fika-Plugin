using System.Runtime.Serialization;

namespace Fika.Core.Networking.Http;

[DataContract]
public struct SetSettingsResponse
{
    [DataMember(Name = "success")]
    public bool Success;
}
