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

        [DataMember(Name = "forceSaveOnDeath")]
        public bool ForceSaveOnDeath;

        public ClientConfigModel(bool useBTR, bool friendlyFire, bool dynamicVExfils, bool allowFreeCam, bool allowItemSending, bool forceSaveOnDeath)
        {
            UseBTR = useBTR;
            FriendlyFire = friendlyFire;
            DynamicVExfils = dynamicVExfils;
            AllowFreeCam = allowFreeCam;
            AllowItemSending = allowItemSending;
            ForceSaveOnDeath = forceSaveOnDeath;
        }

        public new void ToString()
        {
            FikaPlugin.Instance.FikaLogger.LogInfo("Received config from server:");
            FieldInfo[] fields = typeof(ClientConfigModel).GetFields();
            foreach (FieldInfo field in fields)
            {
                object value = field.GetValue(this);
                FikaPlugin.Instance.FikaLogger.LogInfo(field.Name + ": " + value);
            }
        }
    }
}