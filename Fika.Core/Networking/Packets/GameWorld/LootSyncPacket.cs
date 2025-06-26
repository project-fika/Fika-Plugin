using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
    public struct LootSyncPacket : INetSerializable
    {
        public LootSyncStruct Data;

        public void Deserialize(NetDataReader reader)
        {
            ref LootSyncStruct data = ref Data;

            data.Id = reader.GetInt();
            data.Position = reader.GetVector3();
            data.Rotation = reader.GetQuaternion();
            data.Done = reader.GetBool();

            if (!data.Done)
            {
                data.Velocity = reader.GetVector3();
                data.AngularVelocity = reader.GetVector3();
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
