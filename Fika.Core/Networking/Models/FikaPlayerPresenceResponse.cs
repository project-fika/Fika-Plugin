using EFT;
using System.Runtime.Serialization;

namespace Fika.Core.Networking.Http
{
	[DataContract]
	public struct FikaPlayerPresence
	{
		[DataMember(Name = "nickname")]
		public string Nickname;

		[DataMember(Name = "level")]
		public int Level;

		[DataMember(Name = "inRaid")]
		public bool InRaid;

		[DataMember(Name = "raidInformation")]
		public RaidInformation? RaidInformation;

		public FikaPlayerPresence(string nickname, int level, bool inRaid, RaidInformation? raidInformation)
		{
			Nickname = nickname;
			Level = level;
			InRaid = inRaid;
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

		public RaidInformation(string location, ESideType side)
		{
			Location = location;
			Side = side;
		}
	}
}
