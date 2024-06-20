using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace Fika.Core.UI.Models
{
    [DataContract]
    public struct ClientConfigModel
    {
        [DataMember(Name = "useBtr")]
        public bool UseBTR;

        [DataMember(Name = "friendlyFire")]
        public bool FriendlyFire;

        [DataMember(Name = "dynamicVExfils")]
        public bool DynamicVExfils;

        [DataMember(Name = "allowFreeCam")]
        public bool AllowFreeCam;

        [DataMember(Name = "allowItemSending")]
        public bool AllowItemSending;

        [DataMember(Name = "blacklistedItems")]
        public string[] BlacklistedItems;

        [DataMember(Name = "forceSaveOnDeath")]
        public bool ForceSaveOnDeath;

        [DataMember(Name = "useInertia")]
        public bool UseInertia;

        public ClientConfigModel(bool useBTR, bool friendlyFire, bool dynamicVExfils, bool allowFreeCam, bool allowItemSending, string[] blacklistedItems, bool forceSaveOnDeath, bool useInertia)
        {
            UseBTR = useBTR;
            FriendlyFire = friendlyFire;
            DynamicVExfils = dynamicVExfils;
            AllowFreeCam = allowFreeCam;
            AllowItemSending = allowItemSending;
            BlacklistedItems = blacklistedItems;
            ForceSaveOnDeath = forceSaveOnDeath;
            UseInertia = useInertia;
        }

        public new void ToString()
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
}