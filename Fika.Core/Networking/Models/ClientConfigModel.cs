using System;
using System.Runtime.Serialization;
using Fika.Core.Main.Utils;

namespace Fika.Core.Networking.Models;

[DataContract]
public struct ClientConfigModel
{
    [DataMember(Name = "useBtr")]
    public bool UseBTR { get; set; }

    [DataMember(Name = "friendlyFire")]
    public bool FriendlyFire { get; set; }

    [DataMember(Name = "dynamicVExfils")]
    public bool DynamicVExfils { get; set; }

    [DataMember(Name = "allowFreeCam")]
    public bool AllowFreeCam { get; set; }

    [DataMember(Name = "AllowSpectateFreeCam")]
    public bool AllowSpectateFreeCam { get; set; }

    [DataMember(Name = "allowItemSending")]
    public bool AllowItemSending { get; set; }

    [DataMember(Name = "blacklistedItems")]
    public string[] BlacklistedItems { get; set; }

    [DataMember(Name = "forceSaveOnDeath")]
    public bool ForceSaveOnDeath { get; set; }

    [DataMember(Name = "useInertia")]
    public bool UseInertia { get; set; }

    [DataMember(Name = "sharedQuestProgression")]
    public bool SharedQuestProgression { get; set; }

    [DataMember(Name = "canEditRaidSettings")]
    public bool CanEditRaidSettings { get; set; }

    [DataMember(Name = "enableTransits")]
    public bool EnableTransits { get; set; }

    [DataMember(Name = "anyoneCanStartRaid")]
    public bool AnyoneCanStartRaid { get; set; }

    [DataMember(Name = "allowNamePlates")]
    public bool AllowNamePlates { get; set; }

    [DataMember(Name = "randomLabyrinthSpawns")]
    public bool RandomLabyrinthSpawns { get; set; }

    [DataMember(Name = "pmcFoundInRaid")]
    public bool PMCFoundInRaid { get; set; }

    [DataMember(Name = "allowSpectateBots")]
    public bool AllowSpectateBots { get; set; }

    [DataMember(Name = "instantLoad")]
    public bool InstantLoad { get; set; }

    [DataMember(Name = "fastLoad")]
    public bool FastLoad { get; set; }

    [DataMember(Name = "reviveConfig")]
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
    [DataMember(Name = "enabled")]
    public bool Enabled { get; set; }

    [DataMember(Name = "headshotKills")]
    public bool HeadshotKills { get; set; }

    [DataMember(Name = "grenadesKills")]
    public bool GrenadesKills { get; set; }

    [DataMember(Name = "allowLooting")]
    public bool AllowLooting { get; set; }

    [DataMember(Name = "maxRevives")]
    public int MaxRevives { get; set; }

    [DataMember(Name = "bleedoutTime")]
    public float BleedoutTime { get; set; }

    [DataMember(Name = "reviveTime")]
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