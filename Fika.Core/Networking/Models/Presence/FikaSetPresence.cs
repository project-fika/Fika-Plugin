using System;
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

		[Obsolete("Currently not used, handled on server", true)]
		public FikaSetPresence(EFikaPlayerPresence presence, RaidInformation? raidInformation)
		{
			Presence = presence;
			RaidInformation = raidInformation;
		}
	}
}
