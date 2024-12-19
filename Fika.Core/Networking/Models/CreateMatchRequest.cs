using EFT;
using JsonType;
using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace Fika.Core.Networking.Http
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

		[DataMember(Name = "gameVersion")]
		public string GameVersion;

		[DataMember(Name = "fikaVersion")]
		public Version FikaVersion;

		[DataMember(Name = "side")]
		public ESideType Side;

		[DataMember(Name = "time")]
		public EDateTime Time;

		[DataMember(Name = "isSpectator")]
		public bool IsSpectator;

		public CreateMatch(string raidCode, string serverId, string hostUsername, bool isSpectator, long timestamp, RaidSettings settings, int expectedNumberOfPlayers, ESideType side, EDateTime time)
		{
			RaidCode = raidCode;
			ServerId = serverId;
			HostUsername = hostUsername;
			Timestamp = timestamp;
			Settings = settings;
			GameVersion = FikaPlugin.EFTVersionMajor;
			FikaVersion = Assembly.GetExecutingAssembly().GetName().Version;
			Side = side;
			Time = time;
			IsSpectator = isSpectator;
		}
	}
}