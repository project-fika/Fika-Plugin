using EFT.NetworkPackets;

namespace Fika.Core.Networking.Packets.World;

public struct KnifeHitPacket : INetSerializable
{
    public int NetId;
    public EHitType HitType;
    public int HitId;
    public Vector3 HitPoint;

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.PutEnum(HitType);
        writer.Put(HitId);
        writer.PutUnmanaged(HitPoint);
    }

    public void Deserialize(NetDataReader reader)
    {
        NetId = reader.GetInt();
        HitType = reader.GetEnum<EHitType>();
        HitId = reader.GetInt();
        HitPoint = reader.GetUnmanaged<Vector3>();
    }
}
