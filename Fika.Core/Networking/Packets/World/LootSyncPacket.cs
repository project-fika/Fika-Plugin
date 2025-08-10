using LiteNetLib.Utils;

namespace Fika.Core.Networking.Packets.World
{
    public struct LootSyncPacket : INetSerializable
    {
        public LootSyncStruct Data;

        public void Deserialize(NetDataReader reader)
        {
            ref LootSyncStruct data = ref Data;

            data.Id = reader.GetInt();
            data.Position = reader.GetStruct<Vector3>();
            data.Rotation = reader.GetStruct<Quaternion>();
            data.Done = reader.GetBool();

            if (!data.Done)
            {
                data.Velocity = reader.GetStruct<Vector3>();
                data.AngularVelocity = reader.GetStruct<Vector3>();
            }
        }

        public readonly void Serialize(NetDataWriter writer)
        {
            writer.Put(Data.Id);
            writer.PutStruct(Data.Position);
            writer.PutStruct(Data.Rotation);
            writer.Put(Data.Done);
            if (!Data.Done)
            {
                writer.PutStruct(Data.Velocity);
                writer.PutStruct(Data.AngularVelocity);
            }
        }
    }
}
