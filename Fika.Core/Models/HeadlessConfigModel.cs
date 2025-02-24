using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

namespace Fika.Core.UI.Models
{
    [DataContract]
    public struct HeadlessProfiles
    {
        [DataMember(Name = "amount")]
        public int Amount { get; set; }

        [DataMember(Name = "aliases")]
        public Dictionary<string, string> Aliases { get; set; }
    }

    [DataContract]
    public struct HeadlessScripts
    {
        [DataMember(Name = "generate")]
        public bool Generate { get; set; }

        [DataMember(Name = "forceIp")]
        public string ForceIp { get; set; }
    }
    
    [DataContract]
    public struct HeadlessConfigModel
    {
        [DataMember(Name = "profiles")]
        public HeadlessProfiles Profiles { get; set; }

        [DataMember(Name = "scripts")]
        public HeadlessScripts Scripts { get; set; }

        [DataMember(Name = "setLevelToAverageOfLobby")]
        public bool SetLevelToAverageOfLobby { get; set; }

        [DataMember(Name = "restartAfterAmountOfRaids")]
        public int RestartAfterAmountOfRaids { get; set; }

        public void LogValues()
        {
            FikaPlugin.Instance.FikaLogger.LogInfo("Received Headless config from server:");
            FieldInfo[] fields = typeof(HeadlessConfigModel).GetFields();
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
}