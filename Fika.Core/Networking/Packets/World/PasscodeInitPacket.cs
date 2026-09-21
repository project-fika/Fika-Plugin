using System.Collections.Generic;

namespace Fika.Core.Networking.Packets.World;

public struct PasscodeInitPacket(bool isRequest) : INetSerializable
{
    public bool IsRequest = isRequest;
    public Dictionary<string, string[]> Hints;

    public void Deserialize(NetDataReader reader)
    {
        IsRequest = reader.GetBool();
        if (IsRequest)
        {
            return;
        }

        var amount = reader.GetUShort();
        Hints = new(amount);
        for (var i = 0; i < amount; i++)
        {
            var passcodeId = reader.GetString();
            var hintAmount = reader.GetUShort();
            var hints = new string[hintAmount];
            for (var j = 0; j < hintAmount; j++)
            {
                hints[j] = reader.GetString();
            }

            Hints[passcodeId] = hints;
        }
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(IsRequest);
        if (IsRequest)
        {
            return;
        }

        writer.Put((ushort)Hints.Count);
        foreach ((var passcodeId, var hints) in Hints)
        {
            writer.Put(passcodeId);
            writer.Put((ushort)hints.Length);
            for (var i = 0; i < hints.Length; i++)
            {
                writer.Put(hints[i]);
            }
        }
    }
}
