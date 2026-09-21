using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Fika.Core.Networking.Models.Admin;

[DataContract]
public struct SetSettingsResponse
{
    [JsonPropertyName("success")]
    public bool Success;
}
