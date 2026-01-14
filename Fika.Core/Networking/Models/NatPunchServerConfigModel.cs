using System;
using System.Runtime.Serialization;

namespace Fika.Core.Networking.Models;

[DataContract]
public struct NatPunchServerConfigModel
{
    [DataMember(Name = "enable")]
    public bool Enable;

    [DataMember(Name = "port")]
    public int Port;

    public NatPunchServerConfigModel(bool enable, int port)
    {
        Enable = enable;
        Port = port;
    }

    public readonly void LogValues()
    {
        FikaPlugin.Instance.FikaLogger.LogInfo("Received NatPunchServer config from server:");
        foreach (var field in typeof(NatPunchServerConfigModel).GetFields())
        {
            var value = field.GetValue(this);
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
                FikaPlugin.Instance.FikaLogger.LogInfo(field.Name + ": " + values);
                continue;
            }
            FikaPlugin.Instance.FikaLogger.LogInfo(field.Name + ": " + value);
        }
    }
}