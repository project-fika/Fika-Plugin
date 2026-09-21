using System;
using System.Runtime.Serialization;
using Fika.Core.Main.Utils;
using System.Text.Json.Serialization;

namespace Fika.Core.Networking.Models;

[DataContract]
public struct NatPunchServerConfigModel
{
    [JsonPropertyName("enable")]
    public bool Enable;

    [JsonPropertyName("port")]
    public int Port;

    public NatPunchServerConfigModel(bool enable, int port)
    {
        Enable = enable;
        Port = port;
    }

    public readonly void LogValues()
    {
        FikaGlobals.LogInfo("Received NatPunchServer config from server:");
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
                FikaGlobals.LogInfo(field.Name + ": " + values);
                continue;
            }
            FikaGlobals.LogInfo(field.Name + ": " + value);
        }
    }
}