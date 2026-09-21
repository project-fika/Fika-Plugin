using System.Collections.Generic;
using System.Runtime.Serialization;
using EFT;
using JsonType;
using System.Text.Json.Serialization;

namespace Fika.Core.UI.Models;

[DataContract]
public struct LobbyEntry(string serverId, string hostUsername, int playerCount,
    LobbyEntry.ELobbyStatus status, string location, ESideType side, EDateTime time,
    Dictionary<string, bool> players, bool isHeadless, string headlessRequesterNickname)
{
    [JsonInclude]
    public string ServerId = serverId;

    [JsonInclude]
    public string HostUsername = hostUsername;

    [JsonInclude]
    public int PlayerCount = playerCount;

    [JsonInclude]
    public ELobbyStatus Status = status;

    [JsonInclude]
    public string Location = location;

    [JsonInclude]
    public ESideType Side = side;

    [JsonInclude]
    public EDateTime Time = time;

    [JsonInclude]
    public Dictionary<string, bool> Players = players;

    [JsonInclude]
    public bool IsHeadless = isHeadless;

    [JsonInclude]
    public string HeadlessRequesterNickname = headlessRequesterNickname;

    public enum ELobbyStatus
    {
        LOADING = 0,
        IN_GAME = 1,
        COMPLETE = 2
    }
}