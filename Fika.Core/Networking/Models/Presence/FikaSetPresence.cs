using System.Runtime.Serialization;
using static Fika.Core.UI.FikaUIGlobals;

namespace Fika.Core.Networking.Models.Presence
{
	[DataContract]
	public struct FikaSetPresence
	{
		[DataMember(Name = "activity")]
		public EFikaPlayerPresence Presence;

		[DataMember(Name = "raidInformation")]
		public RaidInformation? RaidInformation;

		public FikaSetPresence(EFikaPlayerPresence presence)
		{
			Presence = presence;
		}

		public FikaSetPresence(EFikaPlayerPresence presence, RaidInformation? raidInformation)
		{
			Presence = presence;
			RaidInformation = raidInformation;
		}
	}
}
