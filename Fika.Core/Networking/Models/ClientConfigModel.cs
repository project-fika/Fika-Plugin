using System;
using System.Runtime.Serialization;
using Fika.Core.Main.Utils;
using System.Text.Json.Serialization;

namespace Fika.Core.Networking.Models;

[DataContract]
public struct ClientConfigModel
{
    [JsonPropertyName("useBtr")]
    public bool UseBTR { get; set; }

    [JsonPropertyName("friendlyFire")]
    public bool FriendlyFire { get; set; }

    [JsonPropertyName("dynamicVExfils")]
    public bool DynamicVExfils { get; set; }

    [JsonPropertyName("allowFreeCam")]
    public bool AllowFreeCam { get; set; }

    [JsonPropertyName("AllowSpectateFreeCam")]
    public bool AllowSpectateFreeCam { get; set; }

    [JsonPropertyName("allowItemSending")]
    public bool AllowItemSending { get; set; }

    [JsonPropertyName("blacklistedItems")]
    public string[] BlacklistedItems { get; set; }

    [JsonPropertyName("forceSaveOnDeath")]
    public bool ForceSaveOnDeath { get; set; }

    [JsonPropertyName("useInertia")]
    public bool UseInertia { get; set; }

    [JsonPropertyName("sharedQuestProgression")]
    public bool SharedQuestProgression { get; set; }

    [JsonPropertyName("canEditRaidSettings")]
    public bool CanEditRaidSettings { get; set; }

    [JsonPropertyName("enableTransits")]
    public bool EnableTransits { get; set; }

    [JsonPropertyName("anyoneCanStartRaid")]
    public bool AnyoneCanStartRaid { get; set; }

    [JsonPropertyName("allowNamePlates")]
    public bool AllowNamePlates { get; set; }

    [JsonPropertyName("randomLabyrinthSpawns")]
    public bool RandomLabyrinthSpawns { get; set; }

    [JsonPropertyName("pmcFoundInRaid")]
    public bool PMCFoundInRaid { get; set; }

    [JsonPropertyName("allowSpectateBots")]
    public bool AllowSpectateBots { get; set; }

    [JsonPropertyName("instantLoad")]
    public bool InstantLoad { get; set; }

    [JsonPropertyName("fastLoad")]
    public bool FastLoad { get; set; }

    [JsonPropertyName("reviveConfig")]
    public ClientReviveConfig ReviveConfig { get; set; }

    public readonly void LogValues()
    {
        FikaGlobals.LogInfo("Received config from server:");
        foreach (var property in typeof(ClientConfigModel).GetProperties())
        {
            var value = property.GetValue(this);
            if (value is Array valueArray)
            {
                var values = "";
                for (var i = 0; i < valueArray.Length; i++)
                {
                    if (i == 0)
                    {
                        values = valueArray.GetValue(i).ToString();
                        continue;
                    }
                    values = values + ", " + valueArray.GetValue(i);
                }
                FikaGlobals.LogInfo($"[Config] {property.Name}: {values}");
                continue;
            }

            if (value is ClientReviveConfig reviveConfig)
            {
                reviveConfig.LogValues();
                continue;
            }

            FikaGlobals.LogInfo($"[Config] {property.Name}: {value}");
        }
    }
}

public struct ClientReviveConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("headshotKills")]
    public bool HeadshotKills { get; set; }

    [JsonPropertyName("grenadesKills")]
    public bool GrenadesKills { get; set; }

    [JsonPropertyName("allowLooting")]
    public bool AllowLooting { get; set; }

    [JsonPropertyName("maxRevives")]
    public int MaxRevives { get; set; }

    [JsonPropertyName("bleedoutTime")]
    public float BleedoutTime { get; set; }

    [JsonPropertyName("reviveTime")]
    public float ReviveTime { get; set; }

    public readonly void LogValues()
    {
        foreach (var property in typeof(ClientReviveConfig).GetProperties())
        {
            var value = property.GetValue(this);
            if (value is Array valueArray)
            {
                var values = "";
                for (var i = 0; i < valueArray.Length; i++)
                {
                    if (i == 0)
                    {
                        values = valueArray.GetValue(i).ToString();
                        continue;
                    }
                    values = values + ", " + valueArray.GetValue(i);
                }
                FikaGlobals.LogInfo($"[ReviveConfig] {property.Name}: {values}");
                continue;
            }

            FikaGlobals.LogInfo($"[ReviveConfig] {property.Name}: {value}");
        }
    }
}