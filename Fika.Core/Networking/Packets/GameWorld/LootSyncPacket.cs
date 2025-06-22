using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
    public struct LootSyncPacket : INetSerializable
    {
        public LootSyncStruct Data;

        public void Deserialize(NetDataReader reader)
        {
            Data = new()
            {
                Id = reader.GetInt(),
                Position = reader.GetVector3(),
                Rotation = reader.GetQuaternion(),
                Done = reader.GetBool()
            };
            if (!Data.Done)
            {
                Data.Velocity = reader.GetVector3();
                Data.AngularVelocity = reader.GetVector3();
            }
        }

        public readonly void Serialize(NetDataWriter writer)
        {
            writer.Put(Data.Id);
            writer.PutVector3(Data.Position);
            writer.PutQuaternion(Data.Rotation);
            writer.Put(Data.Done);
            if (!Data.Done)
            {
                writer.PutVector3(Data.Velocity);
                writer.PutVector3(Data.AngularVelocity);
            }
        }
    }
}
