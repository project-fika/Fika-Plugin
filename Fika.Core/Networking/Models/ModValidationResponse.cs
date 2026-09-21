using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Fika.Core.Networking.Models;

[DataContract]
public struct ModValidationResponse
{
    [JsonPropertyName("forbidden")]
    public string[] Forbidden;

    [JsonPropertyName("missingRequired")]
    public string[] MissingRequired;

    [JsonPropertyName("hashMismatch")]
    public string[] HashMismatch;
}