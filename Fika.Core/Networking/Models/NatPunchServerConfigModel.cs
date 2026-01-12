using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace Fika.Core.Networking.Models;

[DataContract]
public struct NatPunchServerConfigModel
{
    [DataMember(Name = "enable")]
    public bool Enable;

    [DataMember(Name = "ip")]
    public string Ip;

    [DataMember(Name = "port")]
    public int Port;

    [DataMember(Name = "natIntroduceAmount")]
    public int NatIntroduceAmount;

    public NatPunchServerConfigModel(bool enable, string ip, int port, int natIntroduceAmount)
    {
        Enable = enable;
        Ip = ip;
        Port = port;
        NatIntroduceAmount = natIntroduceAmount;
    }

    public readonly void LogValues()
    {
        FikaPlugin.Instance.FikaLogger.LogInfo("Received NatPunchServer config from server:");
        FieldInfo[] fields = typeof(NatPunchServerConfigModel).GetFields();
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