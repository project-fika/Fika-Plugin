using EFT;
using System.Runtime.Serialization;

namespace Fika.Core.Networking.Http
{
	[DataContract]
	public struct RaidSettingsResponse(bool metabolismDisabled, int playersSpawnPlace, int hourOfDay, int timeFlowType)
	{
		[DataMember(Name = "metabolismDisabled")]
		public bool MetabolismDisabled = metabolismDisabled;
		[DataMember(Name = "playersSpawnPlace")]
		public EPlayersSpawnPlace PlayersSpawnPlace = (EPlayersSpawnPlace)playersSpawnPlace;
		[DataMember(Name = "hourOfDay")]
		public int HourOfDay = hourOfDay;
		[DataMember(Name = "timeFlowType")]
		public ETimeFlowType TimeFlowType = (ETimeFlowType)timeFlowType;
	}
}