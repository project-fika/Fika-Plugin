using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Fika.Core.Networking.Models;

[DataContract]
public struct MatchJoinResponse
{
    [JsonPropertyName("gameVersion")]
    public string GameVersion;

    [JsonPropertyName("crc32")]
    public uint Crc32;

    public MatchJoinResponse(string gameVersion, uint crc32)
    {
        GameVersion = gameVersion;
        Crc32 = crc32;
    }
}