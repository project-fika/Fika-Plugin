using LiteNetLib.Utils;

namespace Fika.Core.Networking.Packets.World
{
    public struct CorpsePositionPacket : INetSerializable
    {
        public RagdollPacketStruct Data;

        public void Deserialize(NetDataReader reader)
        {
            Data = new()
            {
                Id = reader.GetInt(),
                Position = reader.GetStruct<Vector3>(),
                Done = reader.GetBool()
            };

            if (Data.Done)
            {
                Data.TransformSyncs = new GStruct135[12];
                for (int i = 0; i < 12; i++)
                {
                    Data.TransformSyncs[i] = new()
                    {
                        Position = reader.GetStruct<Vector3>(),
                        Rotation = reader.GetStruct<Quaternion>()
                    };
                }
            }
        }

        public readonly void Serialize(NetDataWriter writer)
        {
            writer.Put(Data.Id);
            writer.PutStruct(Data.Position);
            writer.Put(Data.Done);

            if (Data.Done && Data.TransformSyncs != null)
            {
                GStruct135[] transforms = Data.TransformSyncs;
                for (int i = 0; i < 12; i++)
                {
                    writer.PutStruct(transforms[i].Position);
                    writer.PutStruct(transforms[i].Rotation);
                }
            }
        }
    }
}
