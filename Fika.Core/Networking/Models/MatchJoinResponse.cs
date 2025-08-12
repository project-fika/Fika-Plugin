using System.Runtime.Serialization;

namespace Fika.Core.Networking.Models;

[DataContract]
public struct MatchJoinResponse
{
    [DataMember(Name = "gameVersion")]
    public string GameVersion;

    [DataMember(Name = "crc32")]
    public uint Crc32;

    public MatchJoinResponse(string gameVersion, uint crc32)
    {
        GameVersion = gameVersion;
        Crc32 = crc32;
    }
}