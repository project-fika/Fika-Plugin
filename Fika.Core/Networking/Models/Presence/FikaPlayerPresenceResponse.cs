using EFT;
using JsonType;
using System.Runtime.Serialization;
using static Fika.Core.UI.FikaUIGlobals;

namespace Fika.Core.Networking.Models.Presence
{
	[DataContract]
	public struct FikaPlayerPresence
	{
		[DataMember(Name = "nickname")]
		public string Nickname;

		[DataMember(Name = "level")]
		public int Level;

		[DataMember(Name = "activity")]
		public EFikaPlayerPresence Activity;

		[DataMember(Name = "activityStartedTimestamp")]
		public long ActivityStartedTimestamp;

		[DataMember(Name = "raidInformation")]
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
		[DataMember(Name = "location")]
		public string Location;

		[DataMember(Name = "side")]
		public ESideType Side;

		[DataMember(Name = "time")]
		public EDateTime Time;

		public RaidInformation(string location, ESideType side, EDateTime time)
		{
			Location = location;
			Side = side;
			Time = time;
		}
	}
}
