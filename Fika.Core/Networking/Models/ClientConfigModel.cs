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
                FikaGlobals.LogInfo(property.Name + ": " + values);
                continue;
            }
            FikaGlobals.LogInfo(property.Name + ": " + value);
        }
    }
}