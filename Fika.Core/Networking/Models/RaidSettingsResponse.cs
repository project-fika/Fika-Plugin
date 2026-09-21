using System.Runtime.Serialization;
using EFT;
using Fika.Core.Main.Utils;
using System.Text.Json.Serialization;

namespace Fika.Core.Networking.Models;

[DataContract]
public struct RaidSettingsResponse(bool received, bool metabolismDisabled,
    FikaCustomRaidSettings customRaidSettings, int playersSpawnPlace, int hourOfDay, int timeFlowType)
{
    [JsonPropertyName("received")]
    public bool Received = received;

    [JsonPropertyName("metabolismDisabled")]
    public bool MetabolismDisabled = metabolismDisabled;

    [JsonPropertyName("customRaidSettings")]
    public FikaCustomRaidSettings CustomRaidSettings = customRaidSettings;

    [JsonPropertyName("playersSpawnPlace")]
    public EPlayersSpawnPlace PlayersSpawnPlace = (EPlayersSpawnPlace)playersSpawnPlace;

    [JsonPropertyName("hourOfDay")]
    public int HourOfDay = hourOfDay;

    [JsonPropertyName("timeFlowType")]
    public ETimeFlowType TimeFlowType = (ETimeFlowType)timeFlowType;
}