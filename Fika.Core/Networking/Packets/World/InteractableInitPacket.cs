using System.Collections.Generic;
using ComponentAce.Compression.Libs.zlib;

namespace Fika.Core.Networking.Packets.World;

public struct InteractableInitPacket(bool isRequest) : INetSerializable
{
    public bool IsRequest = isRequest;
    public byte[] RawData;
    public Dictionary<string, int> Interactables;

    public void Deserialize(NetDataReader reader)
    {
        IsRequest = reader.GetBool();
        if (!IsRequest)
        {
            RawData = reader.GetByteArray();
            Interactables = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(SimpleZlib.Decompress(RawData, null));
        }
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(IsRequest);
        if (!IsRequest)
        {
            var data = SimpleZlib.CompressToBytes(System.Text.Json.JsonSerializer.Serialize(Interactables), 6);
            writer.PutByteArray(data);
        }
    }
}
