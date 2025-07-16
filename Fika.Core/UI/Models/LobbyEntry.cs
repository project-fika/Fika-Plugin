using EFT;
using JsonType;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Fika.Core.UI.Models
{
    [DataContract]
    public struct LobbyEntry(string serverId, string hostUsername, int playerCount,
        LobbyEntry.ELobbyStatus status, string location, ESideType side, EDateTime time,
        Dictionary<string, bool> players, bool isHeadless, string headlessRequesterNickname)
    {
        [DataMember]
        public string ServerId = serverId;

        [DataMember]
        public string HostUsername = hostUsername;

        [DataMember]
        public int PlayerCount = playerCount;

        [DataMember]
        public ELobbyStatus Status = status;

        [DataMember]
        public string Location = location;

        [DataMember]
        public ESideType Side = side;

        [DataMember]
        public EDateTime Time = time;

        [DataMember]
        public Dictionary<string, bool> Players = players;

        [DataMember]
        public bool IsHeadless = isHeadless;

        [DataMember]
        public string HeadlessRequesterNickname = headlessRequesterNickname;

        public enum ELobbyStatus
        {
            LOADING = 0,
            IN_GAME = 1,
            COMPLETE = 2
        }
    }
}