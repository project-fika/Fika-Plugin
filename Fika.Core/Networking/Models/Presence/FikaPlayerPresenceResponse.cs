using System.Runtime.Serialization;
using EFT;
using JsonType;
using static Fika.Core.UI.FikaUIGlobals;
using System.Text.Json.Serialization;

namespace Fika.Core.Networking.Models.Presence;

[DataContract]
public struct FikaPlayerPresence
{
    [JsonPropertyName("nickname")]
    public string Nickname;

    [JsonPropertyName("level")]
    public int Level;

    [JsonPropertyName("activity")]
    public EFikaPlayerPresence Activity;

    [JsonPropertyName("activityStartedTimestamp")]
    public long ActivityStartedTimestamp;

    [JsonPropertyName("raidInformation")]
    public RaidInformation? RaidInformation;

    public FikaPlayerPresence(string nickname, int level, EFikaPlayerPresence activity, long activityStartedTimestamp, RaidInformation? raidInformation)
    {
        Nickname = nickname;
        Level = level;
        Activity = activity;
        ActivityStartedTimestamp = activityStartedTimestamp;
        RaidInformation = raidInformation;
    }
}

[DataContract]
public struct RaidInformation
{
    [JsonPropertyName("location")]
    public string Location;

    [JsonPropertyName("side")]
    public ESideType Side;

    [JsonPropertyName("time")]
    public EDateTime Time;

    [JsonPropertyName("started")]
    public bool Started;

    [JsonPropertyName("matchId")]
    public string MatchId;

    public RaidInformation(string location, ESideType side, EDateTime time, bool started, string matchId)
    {
        Location = location;
        Side = side;
        Time = time;
        Started = started;
        MatchId = matchId;
    }
}
