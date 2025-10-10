using System;
using System.Reflection;
using System.Runtime.Serialization;

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

    public readonly void LogValues()
    {
        FikaPlugin.Instance.FikaLogger.LogInfo("Received config from server:");
        FieldInfo[] fields = typeof(ClientConfigModel).GetFields();
        foreach (FieldInfo field in fields)
        {
            object value = field.GetValue(this);
            if (value is Array valueArray)
            {
                string values = "";
                for (int i = 0; i < valueArray.Length; i++)
                {
                    if (i == 0)
                    {
                        values = valueArray.GetValue(i).ToString();
                        continue;
                    }
                    values = values + ", " + valueArray.GetValue(i).ToString();
                }
                FikaPlugin.Instance.FikaLogger.LogInfo(field.Name + ": " + values);
                continue;
            }
            FikaPlugin.Instance.FikaLogger.LogInfo(field.Name + ": " + value);
        }
    }
}