using EFT.Airdrop;
using EFT.SynchronizableObjects;
using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
    public struct AirdropUpdatePacket : INetSerializable
    {
        public AirplaneDataPacketStruct Data;

        public void Deserialize(NetDataReader reader)
        {
            ref AirplaneDataPacketStruct data = ref Data;
            data.ObjectId = reader.GetInt();
            data.Position = reader.GetVector3();
            data.Rotation = reader.GetVector3();
            data.ObjectType = (SynchronizableObjectType)reader.GetByte();

            if (data.ObjectType == SynchronizableObjectType.AirDrop)
            {
                ref GStruct39 airdrop = ref data.PacketData.AirdropDataPacket;
                airdrop.SignalFire = reader.GetBool();
                airdrop.FallingStage = (EAirdropFallingStage)reader.GetByte();
                airdrop.AirdropType = (EAirdropType)reader.GetByte();
                airdrop.UniqueId = reader.GetInt();
            }
            else
            {
                data.PacketData.AirplaneDataPacket.AirplanePercent = reader.GetInt();
            }

            byte flags = reader.GetByte();
            data.Outdated = (flags & (1 << 0)) != 0;
            data.IsStatic = (flags & (1 << 1)) != 0;
        }

        public readonly void Serialize(NetDataWriter writer)
        {
            writer.Put(Data.ObjectId);
            writer.PutVector3(Data.Position);
            writer.PutVector3(Data.Rotation);
            writer.Put((byte)Data.ObjectType);

            if (Data.ObjectType == SynchronizableObjectType.AirDrop)
            {
                GStruct39 airdrop = Data.PacketData.AirdropDataPacket;
                writer.Put(airdrop.SignalFire);
                writer.Put((byte)airdrop.FallingStage);
                writer.Put((byte)airdrop.AirdropType);
                writer.Put(airdrop.UniqueId);
            }
            else
            {
                writer.Put(Data.PacketData.AirplaneDataPacket.AirplanePercent);
            }

            byte flags = 0;
            if (Data.Outdated) flags |= 1 << 0;
            if (Data.IsStatic) flags |= 1 << 1;
            writer.Put(flags);
        }
    }
}
