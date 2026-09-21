using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Fika.Core.Networking.Models;

[DataContract]
public struct CheckVersionResponse(string version)
{
    [JsonPropertyName("version")]
    public string Version = version;
}