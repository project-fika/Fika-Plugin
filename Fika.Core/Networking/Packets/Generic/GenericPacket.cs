// © 2025 Lacyway All Rights Reserved

using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.Generic;

/// <summary>
/// Packet used for many different things to reduce packet bloat
/// </summary>
/// <param name="packageType"></param>
public class GenericPacket : INetReusable
{
    public int NetId;
    public EGenericSubPacketType Type;
    public IPoolSubPacket SubPacket;

    public void Execute()
    {
        SubPacket.Execute();
    }

    public void Deserialize(NetDataReader reader)
    {
        NetId = reader.GetInt();
        Type = reader.GetEnum<EGenericSubPacketType>();
        SubPacket = GenericSubPacketPoolManager.Instance.GetPacket<IPoolSubPacket>(Type);
        SubPacket.Deserialize(reader);
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.PutEnum(Type);
        SubPacket?.Serialize(writer);
    }

    public void Clear()
    {
        if (SubPacket != null)
        {
            GenericSubPacketPoolManager.Instance.ReturnPacket(Type, SubPacket);
            SubPacket = null;
            Type = default;
        }
    }

    public void Flush()
    {
        GenericSubPacketPoolManager.Instance.ReturnPacket(Type, SubPacket);
        SubPacket = null;
        Type = default;
    }
}
