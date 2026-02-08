using System.Runtime.Serialization;
using EFT;
using Fika.Core.Main.Utils;

namespace Fika.Core.Networking.Models;

[DataContract]
public struct RaidSettingsResponse(bool received, bool metabolismDisabled,
    FikaCustomRaidSettings customRaidSettings, int playersSpawnPlace, int hourOfDay, int timeFlowType)
{
    [DataMember(Name = "received")]
    public bool Received = received;

    [DataMember(Name = "metabolismDisabled")]
    public bool MetabolismDisabled = metabolismDisabled;

    [DataMember(Name = "customRaidSettings")]
    public FikaCustomRaidSettings CustomRaidSettings = customRaidSettings;

    [DataMember(Name = "playersSpawnPlace")]
    public EPlayersSpawnPlace PlayersSpawnPlace = (EPlayersSpawnPlace)playersSpawnPlace;

    [DataMember(Name = "hourOfDay")]
    public int HourOfDay = hourOfDay;

    [DataMember(Name = "timeFlowType")]
    public ETimeFlowType TimeFlowType = (ETimeFlowType)timeFlowType;
}