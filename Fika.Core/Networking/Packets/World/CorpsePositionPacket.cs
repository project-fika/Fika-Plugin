using EFT;
using JsonType;
namespace Fika.Core.Networking.Packets.World;

public struct CorpsePositionPacket : INetSerializable
{
    private const float PositionScale = 8192f;
    private const float RotationScale = short.MaxValue;

    public CorpseSyncPacket Data;
    public float RemoteTime;

    public void Deserialize(NetDataReader reader)
    {
        Data = new()
        {
            Id = reader.GetInt(),
            Position = reader.GetUnmanaged<Vector3>(),
            Done = reader.GetBool()
        };
        RemoteTime = reader.GetFloat();

        CorpseTransformSyncs transforms = new();
        for (var i = 0; i < CorpseTransformSyncs.Count; i++)
        {
            var position = new Vector3(reader.GetShort(), reader.GetShort(), reader.GetShort()) / PositionScale;
            var rotation = new Quaternion(reader.GetShort() / RotationScale, reader.GetShort() / RotationScale,
                reader.GetShort() / RotationScale, reader.GetShort() / RotationScale);
            transforms.Set(i, new()
            {
                Position = Data.Position + position,
                Rotation = rotation
            });
        }

        Data.TransformSyncs = transforms;
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(Data.Id);
        writer.PutUnmanaged(Data.Position);
        writer.Put(Data.Done);
        writer.Put(RemoteTime);

        var transforms = Data.TransformSyncs;
        for (var i = 0; i < CorpseTransformSyncs.Count; i++)
        {
            var transform = transforms.Get(i);
            var position = transform.Position - Data.Position;
            writer.Put(Pack(position.x * PositionScale));
            writer.Put(Pack(position.y * PositionScale));
            writer.Put(Pack(position.z * PositionScale));
            writer.Put(Pack(transform.Rotation.x * RotationScale));
            writer.Put(Pack(transform.Rotation.y * RotationScale));
            writer.Put(Pack(transform.Rotation.z * RotationScale));
            writer.Put(Pack(transform.Rotation.w * RotationScale));
        }
    }

    private static short Pack(float value)
    {
        return (short)Mathf.Clamp(Mathf.Round(value), short.MinValue, short.MaxValue);
    }
}
