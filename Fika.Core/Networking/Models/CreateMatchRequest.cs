using EFT;
using JsonType;
using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace Fika.Core.Networking.Http.Models
{
    [DataContract]
    public struct CreateMatch
    {
        [DataMember(Name = "raidCode")]
        public string RaidCode;
        
        [DataMember(Name = "serverId")]
        public string ServerId;

        [DataMember(Name = "hostUsername")]
        public string HostUsername;

        [DataMember(Name = "timestamp")]
        public long Timestamp;

        [DataMember(Name = "settings")]
        public RaidSettings Settings;

        [DataMember(Name = "expectedNumberOfPlayers")]
        public int ExpectedNumberOfPlayers;

        [DataMember(Name = "gameVersion")]
        public string GameVersion;

        [DataMember(Name = "fikaVersion")]
        public Version FikaVersion;

        [DataMember(Name = "side")]
        public ESideType Side;

        [DataMember(Name = "time")]
        public EDateTime Time;

        public CreateMatch(string raidCode, string serverId, string hostUsername, long timestamp, RaidSettings settings, int expectedNumberOfPlayers, ESideType side, EDateTime time)
        {
            RaidCode = raidCode;
            ServerId = serverId;
            HostUsername = hostUsername;
            Timestamp = timestamp;
            Settings = settings;
            ExpectedNumberOfPlayers = expectedNumberOfPlayers;
            GameVersion = FikaPlugin.EFTVersionMajor;
            FikaVersion = Assembly.GetExecutingAssembly().GetName().Version;
            Side = side;
            Time = time;
        }
    }
}