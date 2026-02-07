namespace Fika.Core.Networking.Packets.World;

public struct LootSyncPacket : INetSerializable
{
    public LootSyncStruct Data;

    public void Deserialize(NetDataReader reader)
    {
        ref var data = ref Data;

        data.Id = reader.GetInt();
        data.Position = reader.GetUnmanaged<Vector3>();
        data.Rotation = reader.GetUnmanaged<Quaternion>();
        data.Done = reader.GetBool();

        if (!data.Done)
        {
            data.Velocity = reader.GetUnmanaged<Vector3>();
            data.AngularVelocity = reader.GetUnmanaged<Vector3>();
        }
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(Data.Id);
        writer.PutUnmanaged(Data.Position);
        writer.PutUnmanaged(Data.Rotation);
        writer.Put(Data.Done);
        if (!Data.Done)
        {
            writer.PutUnmanaged(Data.Velocity);
            writer.PutUnmanaged(Data.AngularVelocity);
        }
    }
}
