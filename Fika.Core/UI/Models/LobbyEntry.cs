using EFT;
using JsonType;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Fika.Core.UI.Models
{
    [DataContract]
    public struct LobbyEntry
    {
        [DataMember]
        public string ServerId;

        [DataMember]
        public string HostUsername;

        [DataMember]
        public int PlayerCount;

        [DataMember]
        public ELobbyStatus Status;

        [DataMember]
        public string Location;

        [DataMember]
        public ESideType Side;

        [DataMember]
        public EDateTime Time;

        [DataMember]
        public Dictionary<string, bool> Players;

        [DataMember]
        public bool IsHeadless;

        [DataMember]
        public string HeadlessRequesterNickname;

        public LobbyEntry(string serverId, string hostUsername, int playerCount, ELobbyStatus status, string location, ESideType side, EDateTime time, Dictionary<string, bool> players, bool isHeadless, string headlessRequesterNickname)
        {
            ServerId = serverId;
            HostUsername = hostUsername;
            PlayerCount = playerCount;
            Status = status;
            Location = location;
            Side = side;
            Time = time;
            Players = players;
            IsHeadless = isHeadless;
            HeadlessRequesterNickname = headlessRequesterNickname;
        }

        public enum ELobbyStatus
        {
            LOADING = 0,
            IN_GAME = 1,
            COMPLETE = 2
        }
    }
}